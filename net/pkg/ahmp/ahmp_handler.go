package ahmp

import (
	"abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/aurl"
	"context"
)

type AhmpHandleEventType int

type AhmpHandler interface {
	OnConnected(ctx context.Context, peer *pcn.Peer) error
	OnConnectFailed(ctx context.Context, address *aurl.AURL) error
	ServeMessage(ctx context.Context, peer *pcn.Peer, frame *pcn.MessageFrame) error
	OnClosed(ctx context.Context, peer *pcn.Peer) error
}

type AhmpFatalHandlerError error //this requires handler reboot from application. otherwise, should exit
type AhmpFatalPeerError error    //this requires peer connection reset.
