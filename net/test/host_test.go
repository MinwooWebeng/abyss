package test

import (
	"abyss/net/pkg/ahmp"
	"abyss/net/pkg/cpb"
	"abyss/net/pkg/host"
	"context"
	"fmt"
	"net"
	"net/http"
	"strconv"
	"testing"
	"time"
)

func TestHost(t *testing.T) {
	hostA_ahmp_handler := ahmp.NewANDHandler("hostA")
	hostA, _ := host.NewHost(context.Background(), "hostA", hostA_ahmp_handler, &cpb.PlayerBackend{}, http.DefaultClient.Jar)
	hostA.ListenAndServeAsync(context.Background())
	hostA_localaddr, _ := hostA.LocalAddr().(*net.UDPAddr)

	hostB_ahmp_handler := ahmp.NewANDHandler("hostB")
	hostB, _ := host.NewHost(context.Background(), "hostB", hostB_ahmp_handler, &cpb.PlayerBackend{}, http.DefaultClient.Jar)
	hostB.ListenAndServeAsync(context.Background())
	hostB_localaddr, _ := hostB.LocalAddr().(*net.UDPAddr)

	response, err := hostA.HttpGet("https://127.0.0.1:" + strconv.Itoa(hostB_localaddr.Port) + "/")
	if err != nil {
		t.Error(err.Error())
	}
	fmt.Println(response.Status)
	response, err = hostB.HttpGet("https://127.0.0.1:" + strconv.Itoa(hostA_localaddr.Port) + "/")
	if err != nil {
		t.Error(err.Error())
	}
	fmt.Println(response.Status)
}

func TestPingHost(t *testing.T) {
	hostA_ping_handler := ahmp.NewPingHandler()
	hostA, _ := host.NewHost(context.Background(), "hostA", hostA_ping_handler, &cpb.PlayerBackend{}, http.DefaultClient.Jar)
	hostA.ListenAndServeAsync(context.Background())

	hostB_ping_handler := ahmp.NewPingHandler()
	hostB, _ := host.NewHost(context.Background(), "hostB", hostB_ping_handler, &cpb.PlayerBackend{}, http.DefaultClient.Jar)
	hostB.ListenAndServeAsync(context.Background())

	hostA.AhmpServer.RequestPeerConnect(hostB.AhmpServer.Dialer.LocalAddress())
	hostB.AhmpServer.RequestPeerConnect(hostA.AhmpServer.Dialer.LocalAddress())

	time.Sleep(time.Second)

	peerB, ok := hostA.AhmpServer.TryGetPeer("hostB")
	if !ok {
		t.Fatal("failed to find peer B")
	}
	select {
	case rtt := <-hostA_ping_handler.PingRTT(peerB):
		fmt.Println("rtt: ", rtt)
	case <-time.After(time.Second * 3):
		t.Fatal("rtt timeout")
	}
}
