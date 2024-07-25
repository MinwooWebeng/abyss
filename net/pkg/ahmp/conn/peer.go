package conn

import (
	"sync/atomic"

	"github.com/quic-go/quic-go"
)

type InboundPrePeer struct {
	Connection quic.Connection
}

type OutboundPrePeer struct {
	Connection quic.Connection
}

type Peer struct {
	InboundConnection  quic.Connection
	OutboundConnection quic.Connection
	AHMPRx             quic.ReceiveStream
	AHMPTx             quic.SendStream

	isClosed atomic.Bool
}

// never call this from Ahmp handler
func (p *Peer) CloseWithError(err error) bool {
	if p.isClosed.CompareAndSwap(false, true) {
		p.InboundConnection.CloseWithError(0, err.Error())
		p.OutboundConnection.CloseWithError(0, err.Error())
		return true
	}
	return false
}

func (p *Peer) Join() {
}
