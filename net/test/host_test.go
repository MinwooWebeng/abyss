package test

import (
	"abyss/net/pkg/ahmp"
	"abyss/net/pkg/cpb"
	"abyss/net/pkg/host"
	"context"
	"fmt"
	"testing"

	"github.com/quic-go/quic-go/http3"
)

func TestHost(t *testing.T) {
	hostA := &host.Host{
		Address:    "127.0.0.1",
		Port:       1906,
		AhmpServer: &ahmp.Server{},
		Http3Server: &http3.Server{
			Handler: &cpb.PlayerBackend{},
		},
	}
	hostA.ListenAndServeAsync(context.Background())

	hostB := &host.Host{
		Address:    "127.0.0.1",
		Port:       1909,
		AhmpServer: &ahmp.Server{},
		Http3Server: &http3.Server{
			Handler: &cpb.PlayerBackend{},
		},
	}
	hostB.ListenAndServeAsync(context.Background())

	response, err := hostA.HttpGet("https://127.0.0.1:1909/")
	if err != nil {
		t.Error(err.Error())
	}
	fmt.Println(response.Status)
	response, err = hostB.HttpGet("https://127.0.0.1:1906/")
	if err != nil {
		t.Error(err.Error())
	}
	fmt.Println(response.Status)
}
