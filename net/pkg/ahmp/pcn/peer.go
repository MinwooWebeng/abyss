package pcn

import (
	"sync"
	"sync/atomic"

	"github.com/quic-go/quic-go"
)

type InboundPrePeer struct {
	Connection quic.Connection
	AHMPRx     quic.ReceiveStream
}

type OutboundPrePeer struct {
	Connection quic.Connection
	AHMPTx     quic.SendStream
}

type Peer struct {
	InboundConnection  quic.Connection
	OutboundConnection quic.Connection
	AHMPRx             quic.ReceiveStream
	AHMPTx             quic.SendStream
	Hash               string

	isClosed     atomic.Bool
	txLock       sync.Mutex
	messageQueue chan MessageFrame
}

func NewPeer(InboundConnection quic.Connection,
	OutboundConnection quic.Connection,
	AHMPRx quic.ReceiveStream,
	AHMPTx quic.SendStream,
	Hash string) *Peer {
	return &Peer{
		InboundConnection:  InboundConnection,
		OutboundConnection: OutboundConnection,
		AHMPRx:             AHMPRx,
		AHMPTx:             AHMPTx,
		Hash:               Hash,

		messageQueue: make(chan MessageFrame),
	}
}

func (p *Peer) SendMessageFrameSync(payload []byte, payload_type FrameType) error {
	p.txLock.Lock()
	defer p.txLock.Unlock()

	return SendMessageFrame(p.AHMPTx, payload, payload_type)
}

// can be called anywhere, the server will automatically detect and issue an global peer disconnect event.
func (p *Peer) CloseWithError(err error) {
	if p.isClosed.CompareAndSwap(false, true) {
		p.AHMPRx.CancelRead(0)
		p.AHMPTx.CancelWrite(0)
		p.InboundConnection.CloseWithError(0, err.Error())
		p.OutboundConnection.CloseWithError(0, err.Error())
	}
}

func (p *Peer) Join() {
}
