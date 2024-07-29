package pcn

import (
	"abyss/net/pkg/aurl"
	"sync"

	"github.com/quic-go/quic-go"
)

type PeerContainer struct {
	mutex  sync.Mutex
	_inner map[string]any
}

func NewPeerContainer() *PeerContainer {
	result := &PeerContainer{
		_inner: make(map[string]any),
	}
	return result
}

type PeerState int

const (
	PartialPeer PeerState = iota + 1
	CompletePeer
)

func (m *PeerContainer) AddInboundConnection(aurl *aurl.AURL, connection quic.Connection, ahmp_rx quic.ReceiveStream) (*Peer, PeerState, bool) {
	m.mutex.Lock()
	defer m.mutex.Unlock()

	entry, ok := m._inner[aurl.Hash]
	if ok {
		switch t := entry.(type) {
		case *Peer:
			return t, CompletePeer, false
		case *InboundPrePeer:
			return nil, PartialPeer, false
		case *OutboundPrePeer:
			new_peer := &Peer{
				InboundConnection:  connection,
				OutboundConnection: t.Connection,
				AHMPRx:             ahmp_rx,
				AHMPTx:             t.AHMPTx,
			}
			m._inner[aurl.Hash] = new_peer
			return new_peer, CompletePeer, true
		default:
			panic("peerContainer: type assertion failed")
		}
	}
	m._inner[aurl.Hash] = &InboundPrePeer{
		Connection: connection,
		AHMPRx:     ahmp_rx,
	}
	return nil, PartialPeer, true
}
func (m *PeerContainer) AddOutboundConnection(aurl *aurl.AURL, connection quic.Connection, ahmp_tx quic.SendStream) (*Peer, PeerState, bool) {
	m.mutex.Lock()
	defer m.mutex.Unlock()

	entry, ok := m._inner[aurl.Hash]
	if ok {
		switch t := entry.(type) {
		case *Peer:
			return t, CompletePeer, false
		case *InboundPrePeer:
			new_peer := &Peer{
				InboundConnection:  t.Connection,
				OutboundConnection: connection,
				AHMPRx:             t.AHMPRx,
				AHMPTx:             ahmp_tx,
			}
			m._inner[aurl.Hash] = new_peer
			return new_peer, CompletePeer, true
		case *OutboundPrePeer:
			return nil, PartialPeer, false
		default:
			panic("peerContainer: type assertion failed")
		}
	}
	m._inner[aurl.Hash] = &OutboundPrePeer{
		Connection: connection,
		AHMPTx:     ahmp_tx,
	}
	return nil, PartialPeer, true
}

func (m *PeerContainer) Get(hash string) (*Peer, bool) {
	m.mutex.Lock()
	defer m.mutex.Unlock()

	res, ok := m._inner[hash]
	if ok {
		peer, ok := res.(*Peer)
		if ok {
			return peer, true
		}
	}
	return nil, false
}

func (m *PeerContainer) Pop(hash string) (*Peer, bool) {
	m.mutex.Lock()
	defer m.mutex.Unlock()

	res, ok := m._inner[hash]
	if ok {
		peer, ok := res.(*Peer)
		if ok {
			delete(m._inner, hash)
			return peer, true
		}
	}
	return nil, false
}
