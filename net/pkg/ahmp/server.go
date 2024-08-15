package ahmp

import (
	pcn "abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/aurl"
	"context"
	"errors"
	"net"

	"github.com/quic-go/quic-go"
)

const NextProtoAhmp = "abyss"

type Dialer interface {
	Dial(remote_addr *aurl.AURL) (quic.Connection, error)
	LocalAddress() *aurl.AURL
}

type Server struct {
	Context       context.Context
	Dialer        Dialer
	AhmpHandler   AhmpHandler
	ErrLog        chan error
	PeerContainer *pcn.PeerContainer
}

// func NewServer(ctx context.Context, dialer Dialer, ahmpHandler AhmpHandler) *Server {
// 	return &Server{
// 		Context:        ctx,
// 		Dialer:         dialer,
// 		AhmpHandler:    ahmpHandler,
// 		ErrLog:         make(chan error, 128),
// 		peer_container: pcn.NewPeerContainer(),
// 	}
// }

type PartialPeerServeError struct {
	Address net.Addr
	Err     error
}

func (e *PartialPeerServeError) Error() string {
	return "PartialPeerServeError(" + e.Address.String() + "):" + e.Err.Error()
}

type PartialPeerConsumeError struct {
	ConnectAURL *aurl.AURL
	Err         error
}

func (e *PartialPeerConsumeError) Error() string {
	return "PartialPeerConsumeError(" + e.ConnectAURL.String() + "):" + e.Err.Error()
}

type AhmpServeError struct {
	PeerHash string
	Err      error
}

func (e *AhmpServeError) Error() string {
	return "AhmpServeError(" + e.PeerHash + "):" + e.Err.Error()
}

func (s *Server) TryLogError(err error) {
	select {
	case s.ErrLog <- err:
	default:
	}
}

func (s *Server) ServeQUICConn(connection quic.Connection) {
	ahmp_stream, err := connection.AcceptUniStream(s.Context)
	if err != nil {
		connection.CloseWithError(0, err.Error())
		s.TryLogError(&PartialPeerServeError{connection.RemoteAddr(), err})
		return
	}

	init_frame, err := pcn.ReceiveMessageFrame(ahmp_stream)
	if err != nil {
		connection.CloseWithError(0, err.Error())
		s.TryLogError(&PartialPeerServeError{connection.RemoteAddr(), err})
		return
	}

	if init_frame.Type != pcn.ID {
		connection.CloseWithError(0, "no init frame")
		s.TryLogError(&PartialPeerServeError{connection.RemoteAddr(), errors.New("no init frame")})
		return
	}
	remote_aurl, err := aurl.ParseAURL(string(init_frame.Payload))
	if err != nil {
		s.TryLogError(&PartialPeerServeError{connection.RemoteAddr(), err})
		return
	}
	//TODO: check init frame

	new_peer, state, ok := s.PeerContainer.AddInboundConnection(remote_aurl, connection, ahmp_stream)
	if !ok {
		connection.CloseWithError(0, "redundant connection")
		s.TryLogError(&PartialPeerServeError{connection.RemoteAddr(), errors.New("redundant connection")})
		return
	}
	if state == pcn.PartialPeer {
		//TODO: may call RequestPeerConnect()
		return
	}
	s.servePeer(new_peer)
}

func (s *Server) ConsumeQUICConn(aurl *aurl.AURL, connection quic.Connection) {
	ahmp_stream, err := connection.OpenUniStreamSync(s.Context)
	if err != nil {
		connection.CloseWithError(0, err.Error())
		s.TryLogError(&PartialPeerConsumeError{aurl, err})
		return
	}

	if err = pcn.SendMessageFrame(ahmp_stream, []byte(s.Dialer.LocalAddress().String()), pcn.ID); err != nil {
		connection.CloseWithError(0, err.Error())
		s.TryLogError(&PartialPeerConsumeError{aurl, err})
		return
	}

	new_peer, state, ok := s.PeerContainer.AddOutboundConnection(aurl, connection, ahmp_stream)
	if !ok {
		connection.CloseWithError(0, "redundant connection")
		s.TryLogError(&PartialPeerConsumeError{aurl, errors.New("redundant connection")})
		return
	}
	if state == pcn.PartialPeer {
		return
	}
	s.servePeer(new_peer)
}

func (s *Server) servePeer(peer *pcn.Peer) {
	err := s.AhmpHandler.OnConnected(s.Context, peer)
	if err != nil {
		s.TryLogError(&AhmpServeError{peer.Address.Hash, err})
	}

	var msg *pcn.MessageFrame
	var err_recv error
	for {
		msg, err_recv = pcn.ReceiveMessageFrame(peer.AHMPRx)
		if err_recv != nil {
			peer.CloseWithError(err_recv)
			break
		}
		err = s.AhmpHandler.ServeMessage(s.Context, peer, msg)
		if err != nil {
			s.TryLogError(&AhmpServeError{peer.Address.Hash, err})
		}
	}

	err_close := s.AhmpHandler.OnClosed(s.Context, peer)
	if err_close != nil {
		s.TryLogError(&AhmpServeError{peer.Address.Hash, err_close})
	}

	//free from peer container. this allows new connection from same peer.
	_, ok := s.PeerContainer.Pop(peer.Address.Hash)
	if !ok {
		panic("servePeer: closing peer is not removed from peer container")
	}
}

func (s *Server) RequestPeerConnect(aurl *aurl.AURL) {
	if aurl.Hash == s.Dialer.LocalAddress().Hash { //prevent self connection
		return
	}
	if _, ok := s.PeerContainer.Get(aurl.Hash); ok { //peer already connected
		return
	}

	go func() {
		connection, err := s.Dialer.Dial(aurl)
		if err != nil {
			conn_fail_err := s.AhmpHandler.OnConnectFailed(s.Context, aurl)
			if conn_fail_err != nil {
				s.TryLogError(&AhmpServeError{aurl.Hash, conn_fail_err})
			}
			return
		}

		s.ConsumeQUICConn(aurl, connection)
	}()
}

func (s *Server) TryGetPeer(hash string) (*pcn.Peer, bool) {
	return s.PeerContainer.Get(hash)
}

func (s *Server) WaitError() error {
	return <-s.ErrLog
}
