package ahmp

import (
	"abyss/net/pkg/ahmp/conn"
	"abyss/net/pkg/aurl"
	"context"
	"errors"

	"github.com/quic-go/quic-go"
)

const NextProtoAhmp = "abyss"

type AhmpHandler interface {
	OnConnected(peer *conn.Peer)
	OnConnectFailed(address *aurl.AbyssURL)
	ServeMessage(peer *conn.Peer, frame *MessageFrame) error
	OnClosed(peer *conn.Peer)
}

type Dialer interface {
	Dial(address *aurl.AbyssURL)
	Consume()
}

type Server struct {
	Context     context.Context
	AhmpHandler AhmpHandler

	//internal
	peer_container *conn.PeerContainer
}

func (s *Server) ServeQUICConn(connection quic.Connection) error {
	//TODO: accept one monodirectional stream.
	ahmp_stream, err := connection.AcceptUniStream(s.Context)
	if err != nil {
		connection.CloseWithError(0, err.Error())
		return err
	}

	init_frame, err := ReceiveMessageFrame(ahmp_stream)
	if err != nil {
		connection.CloseWithError(0, err.Error())
		return err
	}

	if init_frame.Type != 0 {
		connection.CloseWithError(0, "no init frame")
		return errors.New("no init frame")
	}
	//TODO: check init frame

	new_peer, state, ok := s.peer_container.AddInboundConnection(aurl.AbyssURL{}, connection)
	if !ok {
		connection.CloseWithError(0, "redundant connection")
		return errors.New("redundant connection")
	}
	if state == conn.PartialPeer {
		//TODO: connect or hold
		return nil
	}
	return s.servePeer(new_peer)
}

func (s *Server) ConsumeQUICConn(connection quic.Connection) error {
	ahmp_stream, err := connection.OpenUniStreamSync(s.Context)
	if err != nil {
		connection.CloseWithError(0, err.Error())
		return err
	}

	if err = SendMessageFrame(ahmp_stream, []byte{}, 0); err != nil {
		connection.CloseWithError(0, err.Error())
		return err
	}

	new_peer, state, ok := s.peer_container.AddOutboundConnection(aurl.AbyssURL{}, connection)
	if !ok {
		connection.CloseWithError(0, "redundant connection")
		return errors.New("redundant connection")
	}
	if state == conn.PartialPeer {
		return nil
	}
	return s.servePeer(new_peer)
}

func (s *Server) ConsumeDialer() {

}

func (s *Server) servePeer(peer *conn.Peer) error {
	s.AhmpHandler.OnConnected(peer)

	for {
		msg, err := ReceiveMessageFrame(peer.AHMPRx)
		if err != nil {
			s.RequestPeerClose(peer, err)
			return err
		}
		s.AhmpHandler.ServeMessage(peer, msg)
	}
}

// this can be called multiple times & inside ahmp handler
func (s *Server) RequestPeerClose(peer *conn.Peer, err error) {
	if peer.CloseWithError(err) {
		go func() {
			s.AhmpHandler.OnClosed(peer)
		}()
	}
}
