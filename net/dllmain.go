package main

import "C"
import (
	"abyss/net/pkg/ahmp"
	"abyss/net/pkg/ahmp/and"
	"abyss/net/pkg/aurl"
	"abyss/net/pkg/cpb"
	"abyss/net/pkg/host"
	"bytes"
	"context"
	"encoding/binary"
	"errors"
	"fmt"
	"io"
	"net/http"
	"runtime/cgo"
)

func init() {
	//TODO
}

const version = "0.1.0"

//export GetVersion
func GetVersion(buf *C.char, buflen C.int) C.int {
	return TryMarshalBytes(buf, buflen, []byte(version))
}

type HostExport struct {
	Host      *host.Host
	CloseFunc context.CancelFunc

	ANDHandler *ahmp.ANDHandler
	ANDEventCh <-chan and.NeighborDiscoveryEvent
}

//export NewAbyssHost
func NewAbyssHost(hash *C.char, hash_len C.int, backend_root *C.char, backend_root_len C.int) C.uintptr_t {
	ctx, cancelFunc := context.WithCancel(context.Background())
	go_hash := string(UnmarshalBytes(hash, hash_len))

	and_event_ch := make(chan and.NeighborDiscoveryEvent, 128)
	and_handler := ahmp.NewANDHandler(ctx, go_hash, and_event_ch)
	player_backend, err := cpb.NewDefaultPlayerBackend(string(UnmarshalBytes(backend_root, backend_root_len)))
	if err != nil {
		return 0
	}
	hostA, err := host.NewHost(ctx, go_hash, and_handler, player_backend, http.DefaultClient.Jar)
	if err != nil {
		return 0
	}
	and_handler.ReserveConnectCallback(hostA.AhmpServer.RequestPeerConnect)
	hostA.ListenAndServeAsync(ctx)

	return C.uintptr_t(cgo.NewHandle(&HostExport{
		Host:      hostA,
		CloseFunc: cancelFunc,

		ANDHandler: and_handler,
		ANDEventCh: and_event_ch,
	}))
}

//export CloseAbyssHost
func CloseAbyssHost(handle C.uintptr_t) {
	export := (cgo.Handle(handle)).Value().(*HostExport)
	export.CloseFunc()
	(cgo.Handle(handle)).Delete()
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
	h := (cgo.Handle(handle)).Value().(*HostExport)
	response, err := h.Host.HttpGet(string(UnmarshalBytes(url, url_len)))
	return MakeHttpResponseExport(response, err)
}

//export HttpHead
func HttpHead(handle C.uintptr_t, url *C.char, url_len C.int) C.uintptr_t {
	h := (cgo.Handle(handle)).Value().(*HostExport)
	response, err := h.Host.HttpHead(string(UnmarshalBytes(url, url_len)))
	return MakeHttpResponseExport(response, err)
}

//export HttpPost
func HttpPost(handle C.uintptr_t, url *C.char, url_len C.int, contentType *C.char, contentType_len C.int, body *C.char, bodylen C.int) C.uintptr_t {
	h := (cgo.Handle(handle)).Value().(*HostExport)
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
