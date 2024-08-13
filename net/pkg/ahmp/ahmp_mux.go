package ahmp

import (
	"abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/aurl"
	"context"
)

// TODO
type AhmpMux struct {
}

func (m *AhmpMux) OnConnected(ctx context.Context, peer *pcn.Peer) error {
	return nil
}
func (m *AhmpMux) OnConnectFailed(ctx context.Context, address *aurl.AURL) error {
	return nil
}
func (m *AhmpMux) ServeMessage(ctx context.Context, peer *pcn.Peer, frame *pcn.MessageFrame) error {
	return nil
}
func (m *AhmpMux) OnClosed(ctx context.Context, peer *pcn.Peer) error {
	return nil
}
