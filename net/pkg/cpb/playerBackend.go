package cpb

import "net/http"

type PlayerBackend struct{}

func (b *PlayerBackend) ServeHTTP(resp_writer http.ResponseWriter, request *http.Request) {
	resp_writer.WriteHeader(http.StatusForbidden)
}
