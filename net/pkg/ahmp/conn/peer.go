package conn

import "github.com/quic-go/quic-go"

type Peer struct {
	inbound_abyss  quic.Connection
	outbound_abyss quic.Connection
	inbound_http3  quic.Connection
	outbound_http3 quic.Connection
}

func (p *Peer) Join() {

}
