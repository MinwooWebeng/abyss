package ahmp

import (
	pcn "abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/aurl"
	"context"
	"errors"
	"fmt"

	"github.com/quic-go/quic-go"
)

const NextProtoAhmp = "abyss"

type Dialer interface {
	Dial(remote_addr *aurl.AURL) (quic.Connection, error)
	LocalAddress() *aurl.AURL
}

type AhmpHandler interface {
	OnConnected(ctx context.Context, peer *pcn.Peer)
	OnConnectFailed(ctx context.Context, address *aurl.AURL)
	ServeMessage(ctx context.Context, peer *pcn.Peer, frame *pcn.MessageFrame)
	OnClosed(ctx context.Context, peer *pcn.Peer)
}

type Server struct {
	Context     context.Context
	Dialer      Dialer
	AhmpHandler AhmpHandler

	//internal
	peer_container *pcn.PeerContainer
}

func NewServer(ctx context.Context, dialer Dialer, ahmpHandler AhmpHandler) *Server {
	return &Server{
		Context:        ctx,
		Dialer:         dialer,
		AhmpHandler:    ahmpHandler,
		peer_container: pcn.NewPeerContainer(),
	}
}

func (s *Server) ServeQUICConn(connection quic.Connection) error {
	//TODO: accept one monodirectional stream.
	ahmp_stream, err := connection.AcceptUniStream(s.Context)
	if err != nil {
		connection.CloseWithError(0, err.Error())
		return err
	}

	init_frame, err := pcn.ReceiveMessageFrame(ahmp_stream)
	if err != nil {
		connection.CloseWithError(0, err.Error())
		return err
	}

	if init_frame.Type != pcn.ID {
		connection.CloseWithError(0, "no init frame")
		return errors.New("no init frame")
	}
	remote_aurl, err := aurl.ParseAURL(string(init_frame.Payload))
	if err != nil {
		return err
	}
	//TODO: check init frame

	new_peer, state, ok := s.peer_container.AddInboundConnection(remote_aurl, connection, ahmp_stream)
	if !ok {
		connection.CloseWithError(0, "redundant connection")
		return errors.New("redundant connection")
	}
	if state == pcn.PartialPeer {
		//TODO: connect or hold
		return nil
	}
	return s.servePeer(new_peer)
}

func (s *Server) ConsumeQUICConn(aurl *aurl.AURL, connection quic.Connection) error {
	ahmp_stream, err := connection.OpenUniStreamSync(s.Context)
	if err != nil {
		connection.CloseWithError(0, err.Error())
		return err
	}

	if err = pcn.SendMessageFrame(ahmp_stream, []byte(s.Dialer.LocalAddress().String()), pcn.ID); err != nil {
		connection.CloseWithError(0, err.Error())
		return err
	}

	new_peer, state, ok := s.peer_container.AddOutboundConnection(aurl, connection, ahmp_stream)
	if !ok {
		connection.CloseWithError(0, "redundant connection")
		return errors.New("redundant connection")
	}
	if state == pcn.PartialPeer {
		return nil
	}
	return s.servePeer(new_peer)
}

func (s *Server) servePeer(peer *pcn.Peer) error {
	fmt.Println("on", s.Dialer.LocalAddress(), ") serve:opponent", peer.Hash)

	s.AhmpHandler.OnConnected(s.Context, peer)
	defer func() {
		s.AhmpHandler.OnClosed(s.Context, peer)

		//free from peer container. this allows new connection from same peer.
		s.peer_container.Pop(peer.Hash)
	}()

	for {
		msg, err := pcn.ReceiveMessageFrame(peer.AHMPRx)
		if err != nil {
			peer.CloseWithError(err)
			return err
		}
		s.AhmpHandler.ServeMessage(s.Context, peer, msg)
	}
}

func (s *Server) RequestPeerConnect(aurl *aurl.AURL) {
	if _, ok := s.peer_container.Get(aurl.Hash); !ok {
		go func() {
			connection, err := s.Dialer.Dial(aurl)
			if err != nil {
				return
			}

			s.ConsumeQUICConn(aurl, connection)
		}()
	}
}

func (s *Server) TryGetPeer(hash string) (*pcn.Peer, bool) {
	return s.peer_container.Get(hash)
}
