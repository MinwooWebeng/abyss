package ahmp

import (
	"abyss/net/pkg/ahmp/and"
	"abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/aurl"
	"context"
)

type ANDHandler struct {
	algorithm *and.NeighborDiscoveryHandler
}

func NewANDHandler(local_hash string) *ANDHandler {
	return &ANDHandler{
		algorithm: and.NewNeighborDiscoveryHandler(local_hash),
	}
}

func (m *ANDHandler) OnConnected(ctx context.Context, peer *pcn.Peer) {

}
func (m *ANDHandler) OnConnectFailed(ctx context.Context, address *aurl.AURL) {

}
func (m *ANDHandler) ServeMessage(ctx context.Context, peer *pcn.Peer, frame *pcn.MessageFrame) {
	switch frame.Type { //TODO: parse message and synchronize between calls.
	case 1:
	case 2:
	case 3:
	case 4:
	case 5:
	case 6:
	case 7:
	case 8:
	}
}
func (m *ANDHandler) OnClosed(ctx context.Context, peer *pcn.Peer) {

}
