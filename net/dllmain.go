package main

import "C"
import (
	"abyss/net/pkg/ahmp"
	"abyss/net/pkg/ahmp/and"
	"abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/ahmp/ws"
	"abyss/net/pkg/aurl"
	"abyss/net/pkg/cpb"
	"abyss/net/pkg/functional"
	"abyss/net/pkg/host"
	"bytes"
	"context"
	"encoding/binary"
	"errors"
	"fmt"
	"io"
	"net/http"
	"runtime/cgo"
	"strings"

	"github.com/google/uuid"
)

func init() {
	//TODO
}

const version = "0.1.0"

//export GetVersion
func GetVersion(buf *C.char, buflen C.int) C.int {
	return TryMarshalBytes(buf, buflen, []byte(version))
}

type ErrorExport struct {
	Err error
}

func MakeErrorExport(err error) C.uintptr_t {
	return C.uintptr_t(cgo.NewHandle(&ErrorExport{
		Err: err,
	}))
}

//export CloseError
func CloseError(error_handle C.uintptr_t) {
	cgo.Handle(error_handle).Delete()
}

//export GetErrorBodyLength
func GetErrorBodyLength(error_handle C.uintptr_t) C.int {
	response := (cgo.Handle(error_handle)).Value().(*ErrorExport)
	return C.int(len(response.Err.Error()))
}

//export GetErrorBody
func GetErrorBody(error_handle C.uintptr_t, buf *C.char, buflen C.int) C.int {
	response := (cgo.Handle(error_handle)).Value().(*ErrorExport)
	return TryMarshalBytes(buf, buflen, []byte(response.Err.Error()))
}

type HostExport struct {
	Host      *host.Host
	CloseFunc context.CancelFunc

	ANDHandler *ahmp.ANDHandler
	ANDEventCh <-chan and.NeighborDiscoveryEvent

	SOMHandler *ahmp.SOMHandler
}

//export NewAbyssHost
func NewAbyssHost(hash *C.char, hash_len C.int, backend_root *C.char, backend_root_len C.int) C.uintptr_t {
	ctx, cancelFunc := context.WithCancel(context.Background())
	go_hash := string(UnmarshalBytes(hash, hash_len))

	peer_container := pcn.NewPeerContainer()
	and_event_ch := make(chan and.NeighborDiscoveryEvent, 128)
	and_handler := ahmp.NewANDHandler(ctx, go_hash, and_event_ch)
	som_handler := ahmp.NewSOMHandler(peer_container)
	player_backend, err := cpb.NewDefaultPlayerBackend(string(UnmarshalBytes(backend_root, backend_root_len)))
	if err != nil {
		return 0
	}

	//construct ahmp mux
	ahmp_mux := ahmp.NewAhmpMux()
	ahmp_mux.Handle(uint64(pcn.ID), uint64(pcn.RST), and_handler)
	ahmp_mux.Handle(uint64(pcn.SOR), uint64(pcn.SOD), som_handler)

	//main host construction
	hostA, err := host.NewHost(ctx, go_hash, peer_container, ahmp_mux, player_backend, http.DefaultClient.Jar)
	if err != nil {
		return 0
	}

	//post attachments
	and_handler.ReserveConnectCallback(hostA.AhmpServer.RequestPeerConnect)

	hostA.ListenAndServeAsync(ctx)

	return C.uintptr_t(cgo.NewHandle(&HostExport{
		Host:      hostA,
		CloseFunc: cancelFunc,

		ANDHandler: and_handler,
		ANDEventCh: and_event_ch,

		SOMHandler: som_handler,
	}))
}

//export CloseAbyssHost
func CloseAbyssHost(handle C.uintptr_t) {
	export := cgo.Handle(handle).Value().(*HostExport)
	export.CloseFunc()
	cgo.Handle(handle).Delete()
}

//export GetAhmpError
func GetAhmpError(host_handle C.uintptr_t) C.uintptr_t {
	return MakeErrorExport(cgo.Handle(host_handle).Value().(*HostExport).Host.AhmpServer.WaitError())
}

//export LocalAddr
func LocalAddr(host_handle C.uintptr_t, buf *C.char, buflen C.int) C.int {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)

	return TryMarshalBytes(buf, buflen, []byte(export.Host.AhmpServer.Dialer.LocalAddress().String()))
}

//export RequestPeerConnect
func RequestPeerConnect(host_handle C.uintptr_t, remoteaurl *C.char, remoteaurl_len C.int) {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)

	aurl, err := aurl.ParseAURL(string(UnmarshalBytes(remoteaurl, remoteaurl_len)))
	if err != nil {
		return
	}
	export.Host.AhmpServer.RequestPeerConnect(aurl)
}

//export DisconnectPeer
func DisconnectPeer(host_handle C.uintptr_t, hash *C.char, hash_len C.int) {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)

	peer, ok := export.Host.AhmpServer.TryGetPeer(string(UnmarshalBytes(hash, hash_len)))
	if !ok {
		return
	}

	peer.CloseWithError(errors.New("application: close request"))
}

//////////////
///  AND   ///
//////////////

//export WaitANDEvent
func WaitANDEvent(host_handle C.uintptr_t, buf *C.char, buf_len C.int) C.int {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)
	buffer := UnmarshalBytes(buf, buf_len)

	event := <-export.ANDEventCh

	message_len := len(event.Message)
	localpath_len := len(event.Localpath)
	peerhash_len := len(event.Peer_hash)
	worldjson_len := 0
	var world_json []byte
	if event.World != nil {
		world_json = event.World.GetJsonBytes()
		worldjson_len = len(world_json)
	}

	if len(buffer) < 9+message_len+localpath_len+peerhash_len+worldjson_len {
		return -1
	}

	//all multibyte types are passed in native endian
	//fixed 9 byte
	buffer[0] = byte(event.EventType)
	binary.NativeEndian.PutUint32(buffer[1:], uint32(event.Status))
	buffer[5] = byte(message_len)
	buffer[6] = byte(localpath_len)
	buffer[7] = byte(peerhash_len)
	buffer[8] = byte(worldjson_len)

	//message (string)
	copy(buffer[9:], event.Message)
	copy(buffer[9+message_len:], event.Localpath)
	copy(buffer[9+message_len+localpath_len:], event.Peer_hash)
	copy(buffer[9+message_len+localpath_len+peerhash_len:], world_json)
	return C.int(9 + message_len + localpath_len + peerhash_len + worldjson_len)
}

//export OpenWorld
func OpenWorld(host_handle C.uintptr_t, path *C.char, path_len C.int, url *C.char, url_len C.int) C.int {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)

	if err := export.ANDHandler.OpenWorld(string(UnmarshalBytes(path, path_len)), ahmp.NewANDWorld(string(UnmarshalBytes(url, url_len)))); err != nil {
		return -1
	}
	return 0
}

//export CloseWorld
func CloseWorld(host_handle C.uintptr_t, path *C.char, path_len C.int) {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)

	export.ANDHandler.CloseWorld(string(UnmarshalBytes(path, path_len)))
}

//export Join
func Join(host_handle C.uintptr_t, localpath *C.char, localpath_len C.int, remoteaurl *C.char, remoteaurl_len C.int) {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)

	aurl, err := aurl.ParseAURL(string(UnmarshalBytes(remoteaurl, remoteaurl_len)))
	if err != nil {
		return
	}
	err = export.ANDHandler.JoinAny(string(UnmarshalBytes(localpath, localpath_len)), aurl)
	if err != nil {
		fmt.Println("err: ", err)
	}
}

//////////////
///  SOM   ///
//////////////

//export SOMRequestService
func SOMRequestService(host_handle C.uintptr_t, peer_hash string, world_uuid string) C.uintptr_t {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)

	err := export.SOMHandler.RequestSOMService(peer_hash, world_uuid)
	if err != nil {
		return MakeErrorExport(err)
	}
	return 0
}

//export SOMInitiateService
func SOMInitiateService(host_handle C.uintptr_t, peer_hash string, world_uuid string) {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)

	export.SOMHandler.InitiateSOMService(peer_hash, world_uuid)
}

//export SOMTerminateService
func SOMTerminateService(host_handle C.uintptr_t, peer_hash string, world_uuid string) {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)

	export.SOMHandler.TerminateSOMService(peer_hash, world_uuid)
}

//export SOMRegisterObject
func SOMRegisterObject(host_handle C.uintptr_t, url, object_uuid string) {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)

	export.SOMHandler.RegisterObject(&ws.SharedObject{
		URL:  url,
		UUID: uuid.MustParse(object_uuid),
	})
}

//export SOMShareObject
func SOMShareObject(host_handle C.uintptr_t, objects_uuid *C.char, objects_uuid_len C.int, world_uuid *C.char, world_uuid_len C.int, peer_uuid *C.char, peer_uuid_len C.int) C.uintptr_t {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)

	err := export.SOMHandler.ShareObject(
		strings.Split(string(UnmarshalBytes(objects_uuid, objects_uuid_len)), " "),
		string(UnmarshalBytes(world_uuid, world_uuid_len)),
		string(UnmarshalBytes(peer_uuid, peer_uuid_len)),
	)
	if err != nil {
		return MakeErrorExport(err)
	}
	return 0
}

type SOMEventExport struct {
	event   *ahmp.SomEvent
	bodylen int
}

func MakeSOMEventExport(som_ev *ahmp.SomEvent) C.uintptr_t {
	//1byte type, 1byte hashlen, 1byte uuidlen,
	//1byte shared object count(max 255)

	//n * 3byte(urllen(2), uuidlen(1)) - SO/SOA
	//or
	//n * 1byte(uuidlen(1)) - SOD

	bodylen := 0
	switch som_ev.Type {
	case ahmp.SomReNew, ahmp.SomAppend:
		bodylen += 4 +
			len(som_ev.SomObjects)*3 +
			functional.Accum_all(som_ev.SomObjects, 0, func(o *ws.SharedObject, accum int) int {
				return accum + len(o.URL) + len(o.UUID.String())
			})
	case ahmp.SomDelete:
		bodylen += 4 +
			len(som_ev.SomObjUUIDs) +
			functional.Accum_all(som_ev.SomObjUUIDs, 0, func(uuid string, accum int) int {
				return accum + len(uuid)
			})
	default:
		panic("SOMWaitEvent: undefined event")
	}

	return C.uintptr_t(cgo.NewHandle(&SOMEventExport{
		event:   som_ev,
		bodylen: bodylen,
	}))
}

//export SOMCloseEvent
func SOMCloseEvent(event_handle C.uintptr_t) {
	cgo.Handle(event_handle).Delete()
}

//export SOMGetEventBodyLength
func SOMGetEventBodyLength(event_handle C.uintptr_t) C.int {
	event := (cgo.Handle(event_handle)).Value().(*SOMEventExport)
	return C.int(event.bodylen)
}

//export SOMGetEventBody
func SOMGetEventBody(event_handle C.uintptr_t, buf *C.char, buflen C.int) C.int {
	event := (cgo.Handle(event_handle)).Value().(*SOMEventExport)

	data := make([]byte, event.bodylen)
	data[0] = byte(event.event.Type)
	data[1] = byte(len(event.event.PeerHash))
	data[2] = byte(len(event.event.WorldUUID))

	switch event.event.Type {
	case ahmp.SomReNew, ahmp.SomAppend:
		data[3] = byte(len(event.event.SomObjects))
		rem := data[4:]
		for _, o := range event.event.SomObjects {
			binary.NativeEndian.PutUint16(rem, uint16(len(o.URL)))
			copy(rem[2:], []byte(o.URL))
			rem = rem[2+len(o.URL):]

			uuid_str := o.UUID.String()
			rem[0] = byte(len(uuid_str))
			copy(rem[1:], []byte(uuid_str))
			rem = rem[1+len(uuid_str):]
		}
	case ahmp.SomDelete:
		data[3] = byte(len(event.event.SomObjUUIDs))
		rem := data[4:]
		for _, uuid := range event.event.SomObjUUIDs {
			rem[0] = byte(len(uuid))
			copy(rem[1:], []byte(uuid))
			rem = rem[1+len(uuid):]
		}
	default:
		panic("SOMGetEventBody: undefined event")
	}

	return TryMarshalBytes(buf, buflen, data)
}

//export SOMWaitEvent
func SOMWaitEvent(host_handle C.uintptr_t) C.uintptr_t {
	export := (cgo.Handle(host_handle)).Value().(*HostExport)

	som_ev := export.SOMHandler.WaitEvent()
	return MakeSOMEventExport(som_ev)
}

//////////////
/// HTTP/3 ///
//////////////

type HttpResponseExport struct {
	Body     []byte
	Response *http.Response
	Err      error
}

func MakeHttpResponseExport(response *http.Response, err error) C.uintptr_t {
	if err != nil {
		return C.uintptr_t(cgo.NewHandle(&HttpResponseExport{
			Err: err,
		}))
	}
	body, err := io.ReadAll(response.Body)
	if err != nil {
		return C.uintptr_t(cgo.NewHandle(&HttpResponseExport{
			Err: err,
		}))
	}
	return C.uintptr_t(cgo.NewHandle(&HttpResponseExport{
		Body:     body,
		Response: response,
		Err:      nil,
	}))
}

//export CloseHttpResponse
func CloseHttpResponse(response_handle C.uintptr_t) {
	export := (cgo.Handle(response_handle)).Value().(*HttpResponseExport)
	export.Response.Body.Close()
	(cgo.Handle(response_handle)).Delete()
}

//export HttpGet
func HttpGet(handle C.uintptr_t, url *C.char, url_len C.int) C.uintptr_t {
	h := cgo.Handle(handle).Value().(*HostExport)
	response, err := h.Host.HttpGet(string(UnmarshalBytes(url, url_len)))
	return MakeHttpResponseExport(response, err)
}

//export HttpHead
func HttpHead(handle C.uintptr_t, url *C.char, url_len C.int) C.uintptr_t {
	h := cgo.Handle(handle).Value().(*HostExport)
	response, err := h.Host.HttpHead(string(UnmarshalBytes(url, url_len)))
	return MakeHttpResponseExport(response, err)
}

//export HttpPost
func HttpPost(handle C.uintptr_t, url *C.char, url_len C.int, contentType *C.char, contentType_len C.int, body *C.char, bodylen C.int) C.uintptr_t {
	h := cgo.Handle(handle).Value().(*HostExport)
	response, err := h.Host.HttpPost(string(UnmarshalBytes(url, url_len)), string(UnmarshalBytes(contentType, contentType_len)), bytes.NewReader(UnmarshalBytes(body, bodylen)))
	return MakeHttpResponseExport(response, err)
}

//export GetReponseStatus
func GetReponseStatus(response_handle C.uintptr_t) C.int {
	response := (cgo.Handle(response_handle)).Value().(*HttpResponseExport)
	if response.Err != nil {
		return -1
	}
	return C.int(response.Response.StatusCode)
}

//export GetReponseBodyLength
func GetReponseBodyLength(response_handle C.uintptr_t) C.int {
	response := (cgo.Handle(response_handle)).Value().(*HttpResponseExport)
	if response.Err != nil {
		return C.int(len(response.Err.Error()))
	}
	return C.int(len(response.Body))
}

//export GetResponseBody
func GetResponseBody(response_handle C.uintptr_t, buf *C.char, buflen C.int) C.int {
	response := (cgo.Handle(response_handle)).Value().(*HttpResponseExport)
	if response.Err != nil {
		return TryMarshalBytes(buf, buflen, []byte(response.Err.Error()))
	}
	return TryMarshalBytes(buf, buflen, response.Body)
}

func main() {}
