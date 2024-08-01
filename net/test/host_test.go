package test

import (
	"abyss/net/pkg/ahmp"
	"abyss/net/pkg/ahmp/and"
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
	hostA_events := make(chan and.NeighborDiscoveryEvent)
	hostA_ahmp_handler := ahmp.NewANDHandler(context.Background(), "hostA", hostA_events)
	hostA, _ := host.NewHost(context.Background(), "hostA", hostA_ahmp_handler, &cpb.PlayerBackend{}, http.DefaultClient.Jar)
	hostA.ListenAndServeAsync(context.Background())
	hostA_localaddr, _ := hostA.LocalAddr().(*net.UDPAddr)

	hostB_events := make(chan and.NeighborDiscoveryEvent)
	hostB_ahmp_handler := ahmp.NewANDHandler(context.Background(), "hostB", hostB_events)
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

func PrintEvent(prefix string, ch <-chan and.NeighborDiscoveryEvent) {
	for {
		event := <-ch
		fmt.Println(prefix, event.Stringify())
	}
}

func TestSimpleNeighbor(t *testing.T) {
	hostA_events := make(chan and.NeighborDiscoveryEvent)
	go PrintEvent(">>> hostA", hostA_events)
	hostA_ahmp_handler := ahmp.NewANDHandler(context.Background(), "hostA", hostA_events)
	hostA, _ := host.NewHost(context.Background(), "hostA", hostA_ahmp_handler, &cpb.PlayerBackend{}, http.DefaultClient.Jar)
	hostA.ListenAndServeAsync(context.Background())
	hostA_ahmp_handler.ReserveConnectCallback(hostA.AhmpServer.RequestPeerConnect)

	hostB_events := make(chan and.NeighborDiscoveryEvent)
	go PrintEvent(">>> hostB", hostB_events)
	hostB_ahmp_handler := ahmp.NewANDHandler(context.Background(), "hostB", hostB_events)
	hostB, _ := host.NewHost(context.Background(), "hostB", hostB_ahmp_handler, &cpb.PlayerBackend{}, http.DefaultClient.Jar)
	hostB.ListenAndServeAsync(context.Background())
	hostB_ahmp_handler.ReserveConnectCallback(hostB.AhmpServer.RequestPeerConnect)

	hostC_events := make(chan and.NeighborDiscoveryEvent)
	go PrintEvent(">>> hostC", hostC_events)
	hostC_ahmp_handler := ahmp.NewANDHandler(context.Background(), "hostC", hostC_events)
	hostC, _ := host.NewHost(context.Background(), "hostC", hostC_ahmp_handler, &cpb.PlayerBackend{}, http.DefaultClient.Jar)
	hostC.ListenAndServeAsync(context.Background())
	hostC_ahmp_handler.ReserveConnectCallback(hostC.AhmpServer.RequestPeerConnect)

	hostD_events := make(chan and.NeighborDiscoveryEvent)
	go PrintEvent(">>> hostD", hostD_events)
	hostD_ahmp_handler := ahmp.NewANDHandler(context.Background(), "hostD", hostD_events)
	hostD, _ := host.NewHost(context.Background(), "hostD", hostD_ahmp_handler, &cpb.PlayerBackend{}, http.DefaultClient.Jar)
	hostD.ListenAndServeAsync(context.Background())
	hostD_ahmp_handler.ReserveConnectCallback(hostD.AhmpServer.RequestPeerConnect)

	hostA.AhmpServer.RequestPeerConnect(hostB.AhmpServer.Dialer.LocalAddress())
	hostB.AhmpServer.RequestPeerConnect(hostA.AhmpServer.Dialer.LocalAddress())

	hostB.AhmpServer.RequestPeerConnect(hostC.AhmpServer.Dialer.LocalAddress())
	hostC.AhmpServer.RequestPeerConnect(hostB.AhmpServer.Dialer.LocalAddress())

	hostC.AhmpServer.RequestPeerConnect(hostD.AhmpServer.Dialer.LocalAddress())
	hostD.AhmpServer.RequestPeerConnect(hostC.AhmpServer.Dialer.LocalAddress())

	time.Sleep(time.Second)
	fmt.Println("A-B-C-D link prepared")

	// UUID string
	// URL  string
	b_home := ahmp.NewANDWorld("https://mallang.home.com/\"")
	hostB_ahmp_handler.OpenWorld("/b_home", b_home)

	time.Sleep(time.Second * 3)
	fmt.Println("B opened home")

	peerB_for_A, _ := hostA.AhmpServer.TryGetPeer("hostB")
	hostA_ahmp_handler.JoinConnected("/a_home", peerB_for_A, "/b_home")

	time.Sleep(time.Second * 3)
	fmt.Println("A joined B")

	peerB_for_C, _ := hostC.AhmpServer.TryGetPeer("hostB")
	hostC_ahmp_handler.JoinConnected("/c_home", peerB_for_C, "/b_home")

	time.Sleep(time.Second * 3)
	fmt.Println("C joined B")

	peerC_for_D, _ := hostD.AhmpServer.TryGetPeer("hostC")
	hostD_ahmp_handler.JoinConnected("/d_home", peerC_for_D, "/c_home")

	time.Sleep(time.Second * 3)
	fmt.Println("D joined C")

	time.Sleep(time.Second * 10)
}
