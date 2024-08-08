package pcn

import (
	"abyss/net/pkg/aurl"
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
	Address            *aurl.AURL

	isClosed     atomic.Bool
	txLock       sync.Mutex
	messageQueue chan MessageFrame
}

func NewPeer(InboundConnection quic.Connection,
	OutboundConnection quic.Connection,
	AHMPRx quic.ReceiveStream,
	AHMPTx quic.SendStream,
	Address *aurl.AURL) *Peer {
	return &Peer{
		InboundConnection:  InboundConnection,
		OutboundConnection: OutboundConnection,
		AHMPRx:             AHMPRx,
		AHMPTx:             AHMPTx,
		Address:            Address,

		messageQueue: make(chan MessageFrame),
	}
}

func (p *Peer) SendMessageFrameSync(payload_type FrameType, payload []byte) error {
	p.txLock.Lock()
	defer p.txLock.Unlock()

	return SendMessageFrame(p.AHMPTx, payload, payload_type)
}

func (p *Peer) SendMessageFrameSync2(payload_type FrameType, payloads ...[]byte) error {
	p.txLock.Lock()
	defer p.txLock.Unlock()

	return SendMessageFrame2(p.AHMPTx, payload_type, payloads...)
}

// can be called anywhere, the server will automatically detect and issue an global peer disconnect event.
func (p *Peer) CloseWithError(err error) {
	if p.isClosed.CompareAndSwap(false, true) {
		p.AHMPRx.CancelRead(0)
		p.AHMPTx.CancelWrite(0)
		if err != nil {
			p.InboundConnection.CloseWithError(0, err.Error())
			p.OutboundConnection.CloseWithError(0, err.Error())
		} else {
			p.InboundConnection.CloseWithError(0, "")
			p.OutboundConnection.CloseWithError(0, "")
		}
	}
}
