package ahmp

import (
	"abyss/net/pkg/ahmp/and"
	pcn "abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/aurl"
	"context"
	"errors"

	"github.com/quic-go/quic-go"
)

const NextProtoAhmp = "abyss"

type Dialer interface {
	Dial(remote_addr *aurl.AURL) (quic.Connection, error)
	LocalAddress() *aurl.AURL
}

type AhmpHandler interface {
	OnConnected(ctx context.Context, peer *pcn.Peer) error
	OnConnectFailed(ctx context.Context, address *aurl.AURL) error
	ServeMessage(ctx context.Context, peer *pcn.Peer, frame *pcn.MessageFrame) error
	OnClosed(ctx context.Context, peer *pcn.Peer) error
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
	//// fmt.Println("ServeQUICConn:", s.Dialer.LocalAddress(), remote_aurl, state)
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
	//// fmt.Println("ConsumeQUICConn:", s.Dialer.LocalAddress(), aurl, state)
	if state == pcn.PartialPeer {
		return nil
	}
	return s.servePeer(new_peer)
}

func (s *Server) servePeer(peer *pcn.Peer) error {
	// fmt.Println("on", s.Dialer.LocalAddress(), ") serve:opponent", peer.Address.Hash)
	var err error
	err = s.AhmpHandler.OnConnected(s.Context, peer)
	//// fmt.Println("OnConnected error: ", err)

	var msg *pcn.MessageFrame
	for {
		msg, err = pcn.ReceiveMessageFrame(peer.AHMPRx)
		if err != nil {
			peer.CloseWithError(err)
			break
		}
		err = s.AhmpHandler.ServeMessage(s.Context, peer, msg)
		//// fmt.Println("ServeMessage error: ", err)
	}

	err_close := s.AhmpHandler.OnClosed(s.Context, peer)
	//// fmt.Println("OnClosed error: ", err_close)

	//free from peer container. this allows new connection from same peer.
	s.peer_container.Pop(peer.Address.Hash)

	if err_close != nil {
		return &and.MultipleError{Errors: []error{err, err_close}}
	}
	return err
}

func (s *Server) RequestPeerConnect(aurl *aurl.AURL) {
	if aurl.Hash == s.Dialer.LocalAddress().Hash { //this happens quite a lot.
		return
	}
	if _, ok := s.peer_container.Get(aurl.Hash); !ok {
		go func() {
			connection, err := s.Dialer.Dial(aurl)
			if err != nil {
				s.AhmpHandler.OnConnectFailed(s.Context, aurl)
				// fmt.Println("conn fail: ", err.Error())
				return
			}

			s.ConsumeQUICConn(aurl, connection)
		}()
	}
}

func (s *Server) TryGetPeer(hash string) (*pcn.Peer, bool) {
	return s.peer_container.Get(hash)
}
