package host

import (
	"abyss/net/pkg/ahmp"
	"context"
	"crypto/ed25519"
	"crypto/rand"
	"crypto/tls"
	"crypto/x509"
	"errors"
	"io"
	"math/big"
	"net"
	"net/http"
	"time"

	"github.com/quic-go/quic-go"
	"github.com/quic-go/quic-go/http3"
)

const (
	HostReady   = 0
	HostRunning = iota + 1
	HostTerminated
)

type Host struct {
	Address  string
	Port     int
	TlsConf  *tls.Config
	QuicConf *quic.Config

	AhmpServer    *ahmp.Server
	Http3Server   *http3.Server
	HttpCookieJar http.CookieJar

	//internal
	transport   quic.Transport
	http3Client *http.Client
	error_log   chan error
}

func (h *Host) ListenAndServeAsync(ctx context.Context) error {
	if h.TlsConf == nil {
		defaultTlsConf, err := NewDefaultTlsConf()
		if err != nil {
			return err
		}
		h.TlsConf = defaultTlsConf
	}
	if h.QuicConf == nil {
		defaultQuicConf, err := NewDefaultQuicConf()
		if err != nil {
			return err
		}
		h.QuicConf = defaultQuicConf
	}

	ip, err := net.ResolveIPAddr("ip", h.Address)
	if err != nil {
		return err
	}
	udpConn, err := net.ListenUDP("udp4", &net.UDPAddr{IP: ip.IP, Port: h.Port})
	if err != nil {
		return err
	}
	h.transport = quic.Transport{
		Conn: udpConn,
	}
	roundTripper := &http3.RoundTripper{
		TLSClientConfig: h.TlsConf,  // set a TLS client config, if desired
		QUICConfig:      h.QuicConf, // QUIC connection options
		Dial: func(ctx context.Context, addr string, tlsConf *tls.Config, quicConf *quic.Config) (quic.EarlyConnection, error) {
			a, err := net.ResolveUDPAddr("udp", addr)
			if err != nil {
				return nil, err
			}
			return h.transport.DialEarly(ctx, a, tlsConf, quicConf)
		},
	}
	h.http3Client = &http.Client{
		Transport: roundTripper,
		Jar:       h.HttpCookieJar,
	}

	//initialize servers
	if h.AhmpServer == nil {
		return errors.New("abyss: host ahmp server not provided")
	}
	if h.Http3Server == nil {
		return errors.New("abyss: host http/3 server not provided")
	}
	h.error_log = make(chan error, 32)
	go func() {
		listener, err := h.transport.ListenEarly(h.TlsConf, h.QuicConf)
		if err != nil {
			select {
			case h.error_log <- err:
			default:
			}
			return
		}
		for {
			connection, err := listener.Accept(ctx)
			if err != nil {
				select {
				case h.error_log <- err:
				default:
				}
				return
			}
			switch connection.ConnectionState().TLS.NegotiatedProtocol {
			case http3.NextProtoH3:
				go h.Http3Server.ServeQUICConn(connection)
			case ahmp.NextProtoAhmp:
				go h.AhmpServer.ServeQUICConn(connection)
			default:
				connection.CloseWithError(ProtocolUnsupported, PeerErrorString(ProtocolUnsupported))
			}
		}
	}()

	return nil
}

func (h *Host) HttpGet(url string) (*http.Response, error) {
	return h.http3Client.Get(url)
}
func (h *Host) HttpHead(url string) (*http.Response, error) {
	return h.http3Client.Head(url)
}
func (h *Host) HttpPost(url, contentType string, body io.Reader) (*http.Response, error) {
	return h.http3Client.Post(url, contentType, body)
}

// helper functions
func NewDefaultTlsConf() (*tls.Config, error) {
	ed25519_public_key, ed25519_private_key, err := ed25519.GenerateKey(rand.Reader)
	if err != nil {
		return nil, err
	}

	template := x509.Certificate{
		SerialNumber:          big.NewInt(0),
		KeyUsage:              x509.KeyUsageCertSign | x509.KeyUsageDigitalSignature | x509.KeyUsageKeyEncipherment,
		ExtKeyUsage:           []x509.ExtKeyUsage{x509.ExtKeyUsageServerAuth},
		BasicConstraintsValid: true,
	}
	derBytes, err := x509.CreateCertificate(rand.Reader, &template, &template, ed25519_public_key, ed25519_private_key)
	if err != nil {
		return nil, err
	}
	result := &tls.Config{
		Certificates: []tls.Certificate{
			{
				Certificate: [][]byte{derBytes},
				PrivateKey:  ed25519_private_key,
			},
		},
		NextProtos:         []string{http3.NextProtoH3, ahmp.NextProtoAhmp},
		InsecureSkipVerify: true,
	}
	return result, nil
}

func NewDefaultQuicConf() (*quic.Config, error) {
	result := &quic.Config{
		MaxIdleTimeout:                time.Minute * 5,
		AllowConnectionWindowIncrease: func(conn quic.Connection, delta uint64) bool { return true },
		MaxIncomingStreams:            1000,
		MaxIncomingUniStreams:         1000,
		KeepAlivePeriod:               time.Minute,
		Allow0RTT:                     true,
		EnableDatagrams:               true,
	}
	return result, nil
}
