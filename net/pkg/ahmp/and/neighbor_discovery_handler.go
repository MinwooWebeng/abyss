package and

import (
	"strconv"
	"strings"
	"time"
)

type NeighborDiscoveryEventType int

const (
	JoinDenied  NeighborDiscoveryEventType = iota
	JoinExpired NeighborDiscoveryEventType = iota
	JoinSuccess NeighborDiscoveryEventType = iota
	PeerJoin    NeighborDiscoveryEventType = iota
	PeerLeave   NeighborDiscoveryEventType = iota
)

type NeighborDiscoveryEvent struct {
	EventType NeighborDiscoveryEventType
	Localpath string                      //can be ""
	Peer_hash string                      //never be nil
	Peer      INeighborDiscoveryPeerBase  //can be nil
	Path      string                      //can be ""
	World     INeighborDiscoveryWorldBase //can be nil
	Status    int
	Message   string
}

type PeerSendError struct {
	Peer INeighborDiscoveryPeerBase
}

func (e *PeerSendError) Error() string {
	return "failed to send() to " + e.Peer.GetHash()
}

type MultipleError struct {
	Errors []error
}

func (e *MultipleError) Error() string {
	all_errstring := make([]string, len(e.Errors))
	for i, e := range e.Errors {
		all_errstring[i] = e.Error()
	}
	return "multiple errors: " + strings.Join(all_errstring, ", ")
}

type INeighborDiscoveryHandler interface {
	ReserveEventListener(listener chan<- NeighborDiscoveryEvent)
	ReserveConnectCallback(func(address any))
	ReserveSNBTimer(func(time.Duration, string))

	OpenWorld(path string, world INeighborDiscoveryWorldBase) error
	CloseWorld(path string) error
	ChangeWorldPath(prev_path string, new_path string) bool
	GetWorld(path string) (INeighborDiscoveryWorldBase, bool)
	JoinConnected(local_path string, peer INeighborDiscoveryPeerBase, path string) error
	JoinAny(local_path string, address any, peer_hash string, path string) error

	Connected(peer INeighborDiscoveryPeerBase) error
	Disconnected(peer_hash string) error //also connect fail.
	OnJN(peer INeighborDiscoveryPeerBase, path string) error
	OnJOK(peer INeighborDiscoveryPeerBase, path string, world INeighborDiscoveryWorldBase, member_addrs []any) error
	OnJDN(peer INeighborDiscoveryPeerBase, path string, status int, message string) error
	OnJNI(peer INeighborDiscoveryPeerBase, world_uuid string, address any, joiner_hash string) error
	OnMEM(peer INeighborDiscoveryPeerBase, world_uuid string) error
	OnSNB(peer INeighborDiscoveryPeerBase, world_uuid string, members_hash []string) error
	OnCRR(peer INeighborDiscoveryPeerBase, world_uuid string, missing_member_hash string) error
	OnRST(peer INeighborDiscoveryPeerBase, world_uuid string) error
	OnWorldErr(peer INeighborDiscoveryPeerBase, world_uuid string) error
	OnSNBTimeout(world_uuid string) error
}

// for testing purpose
func (e *NeighborDiscoveryEvent) Stringify() string {
	var sb strings.Builder
	switch e.EventType {
	case JoinDenied:
		sb.WriteString("JoinDenied ")
	case JoinExpired:
		sb.WriteString("JoinExpired ")
	case JoinSuccess:
		sb.WriteString("JoinSuccess ")
	case PeerJoin:
		sb.WriteString("PeerJoin ")
	case PeerLeave:
		sb.WriteString("PeerLeave ")
	}
	sb.WriteString(e.Localpath)
	sb.WriteString(",")
	sb.WriteString(e.Peer_hash)
	sb.WriteString(",")
	sb.WriteString(e.Path)
	if e.World != nil {
		sb.WriteString(",")
		sb.WriteString(e.World.GetUUID())
	}
	if e.Status != 0 {
		sb.WriteString(",")
		sb.WriteString(strconv.Itoa(e.Status))
		sb.WriteString(",")
		sb.WriteString(e.Message)
	}
	return sb.String()
}
