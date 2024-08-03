package cpb

import (
	"net/http"
	"os"
)

type PlayerBackend struct {
	mux *http.ServeMux
}

func NewDefaultPlayerBackend(static_path string) (*PlayerBackend, error) {
	if _, err := os.Stat(static_path); err != nil {
		return nil, err
	}

	result := &PlayerBackend{
		mux: http.NewServeMux(),
	}
	result.mux.Handle("GET /static/", http.StripPrefix("/static/", http.FileServer(http.Dir(static_path))))

	//any other
	result.mux.HandleFunc("/", func(resp_writer http.ResponseWriter, request *http.Request) {
		resp_writer.WriteHeader(http.StatusNotFound)
	})
	return result, nil
}

func (b *PlayerBackend) ServeHTTP(resp_writer http.ResponseWriter, request *http.Request) {
	b.mux.ServeHTTP(resp_writer, request)
}
