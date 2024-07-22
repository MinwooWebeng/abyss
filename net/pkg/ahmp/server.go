package ahmp

import "github.com/quic-go/quic-go"

const NextProtoAhmp = "abyss"

type Server struct {
}

func (s *Server) ServeQUICConn(conn quic.Connection) error {
	//TODO
	return nil
}
