package main

import "C"
import (
	"abyss/net/pkg/ahmp"
	"abyss/net/pkg/cpb"
	"abyss/net/pkg/host"
	"bytes"
	"context"
	"io"
	"net/http"
	"runtime/cgo"

	"github.com/quic-go/quic-go/http3"
)

func init() {
	//TODO
}

const version = "0.9.0"

//export GetVersion
func GetVersion(buf *C.char, buflen C.int) C.int {
	return TryMarshalBytes(buf, buflen, []byte(version))
}

type HostExport struct {
	Host  *host.Host
	Close context.CancelFunc
}

//export NewAbyssHost
func NewAbyssHost() C.uintptr_t {
	ctx, cancelFunc := context.WithCancel(context.Background())
	result := &HostExport{
		Host: &host.Host{
			Address:    "127.0.0.1",
			Port:       1906,
			AhmpServer: &ahmp.Server{},
			Http3Server: &http3.Server{
				Handler: &cpb.PlayerBackend{},
			},
		},
		Close: cancelFunc,
	}
	result.Host.ListenAndServeAsync(ctx)

	return C.uintptr_t(cgo.NewHandle(result))
}

//export CloseAbyssHost
func CloseAbyssHost(handle C.uintptr_t) {
	export := (cgo.Handle(handle)).Value().(*HostExport)
	export.Close()
	(cgo.Handle(handle)).Delete()
}

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
		Err:      err,
	}))
}

//export HttpGet
func HttpGet(handle C.uintptr_t, url string) C.uintptr_t {
	h := (cgo.Handle(handle)).Value().(*HostExport)
	response, err := h.Host.HttpGet(url)
	return MakeHttpResponseExport(response, err)
}

//export HttpHead
func HttpHead(handle C.uintptr_t, url string) C.uintptr_t {
	h := (cgo.Handle(handle)).Value().(*HostExport)
	response, err := h.Host.HttpHead(url)
	return MakeHttpResponseExport(response, err)
}

//export HttpPost
func HttpPost(handle C.uintptr_t, url, contentType string, body *C.char, bodylen C.int) C.uintptr_t {
	h := (cgo.Handle(handle)).Value().(*HostExport)
	response, err := h.Host.HttpPost(url, contentType, bytes.NewReader(UnmarshalBytes(body, bodylen)))
	return MakeHttpResponseExport(response, err)
}

//export CloseHttpResponse
func CloseHttpResponse(handle C.uintptr_t) {
	export := (cgo.Handle(handle)).Value().(*HttpResponseExport)
	export.Response.Body.Close()
	(cgo.Handle(handle)).Delete()
}

//export GetReponseBodyLength
func GetReponseBodyLength(handle C.uintptr_t) C.int {
	response := (cgo.Handle(handle)).Value().(*HttpResponseExport)
	return C.int(len(response.Body))
}

// export GetResponseBody
func GetResponseBody(handle C.uintptr_t, buf *C.char, buflen C.int) C.int {
	response := (cgo.Handle(handle)).Value().(*HttpResponseExport)
	return TryMarshalBytes(buf, buflen, response.Body)
}

func main() {}
