package host

import (
	"abyss/net/pkg/ahmp"
	"abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/aurl"
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
	"net/netip"
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
	LocalIdentity string //TODO: extend this to local secret key, TLS signed certificate for local public key, name, etc.
	QuicTransport *quic.Transport
	TlsConf       *tls.Config
	QuicConf      *quic.Config

	AhmpServer    *ahmp.Server
	Http3Server   *http3.Server
	HttpCookieJar http.CookieJar

	//internal
	http3Client *http.Client
	error_log   chan error
}

func NewHost(ctx context.Context, local_identity string, peer_container *pcn.PeerContainer, ahmp_handler ahmp.AhmpHandler, http_handler http.Handler, cookie_jar http.CookieJar) (*Host, error) {
	h := &Host{
		LocalIdentity: local_identity,
	}
	udpConn, err := net.ListenUDP("udp4", net.UDPAddrFromAddrPort(netip.MustParseAddrPort("0.0.0.0:0")))
	if err != nil {
		return nil, err
	}
	h.QuicTransport = &quic.Transport{Conn: udpConn}
	defaultTlsConf, err := NewDefaultTlsConf()
	if err != nil {
		return nil, err
	}
	h.TlsConf = defaultTlsConf
	defaultQuicConf, err := NewDefaultQuicConf()
	if err != nil {
		return nil, err
	}
	h.QuicConf = defaultQuicConf
	h.AhmpServer = &ahmp.Server{
		Context:       ctx,
		Dialer:        NewDefaultDialer(ctx, h.QuicTransport, h.TlsConf, h.QuicConf, h.LocalIdentity),
		AhmpHandler:   ahmp_handler,
		ErrLog:        make(chan error, 64),
		PeerContainer: peer_container,
	}
	h.Http3Server = &http3.Server{
		Handler: http_handler,
	}
	h.HttpCookieJar = cookie_jar

	//initialize http3 client
	roundTripper := &http3.RoundTripper{
		TLSClientConfig: h.TlsConf,  // set a TLS client config, if desired
		QUICConfig:      h.QuicConf, // QUIC connection options
		Dial: func(ctx context.Context, addr string, tlsConf *tls.Config, quicConf *quic.Config) (quic.EarlyConnection, error) {
			//addr is {peer hash} + ".null:443", hacky tactic explained below.
			var abyst_hash = addr[:len(addr)-9]
			//fmt.Println("Http3 Dial peer:", abyst_hash)

			peer, ok := h.AhmpServer.TryGetPeer(abyst_hash) //trim .abyst
			if !ok {
				//peer not found, unable to dial
				return nil, errors.New("abyst: peer not found")
			}

			early_conn, err := h.QuicTransport.DialEarly(ctx, peer.OutboundConnection.RemoteAddr(), tlsConf, quicConf)
			if err != nil {
				return nil, err
			}

			//TODO: abyssh3 address identity check. compare TLS certificate from early connection and previous ahmp handshake.
			//currently, man-in-the-middle attack is possible

			return early_conn, nil
		},
	}
	h.http3Client = &http.Client{
		Transport: roundTripper,
		Jar:       h.HttpCookieJar,
	}
	return h, nil
}

func (h *Host) LocalAddr() net.Addr {
	return h.QuicTransport.Conn.LocalAddr()
}

func (h *Host) ListenAndServeAsync(ctx context.Context) error {
	if h.QuicTransport == nil ||
		h.TlsConf == nil ||
		h.QuicConf == nil ||
		h.AhmpServer == nil ||
		h.Http3Server == nil {
		return errors.New("ListenAndServeAsync: incomplete server")
	}

	h.error_log = make(chan error, 32)
	go func() {
		listener, err := h.QuicTransport.ListenEarly(h.TlsConf, h.QuicConf)
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

// puts aurl hash into url user section
func HackyAddressTransformAurlToURL(aurl_str string) (string, error) {
	aurl, err := aurl.ParseAURL(aurl_str)
	if err != nil {
		return "", err
	}
	if aurl.Scheme != "abyst" {
		return "", errors.New("url scheme mismatch")
	}

	return "https://" + aurl.Hash + ".null" + aurl.Path, nil
}

func (h *Host) HttpGet(aurl_str string) (*http.Response, error) {
	url, err := HackyAddressTransformAurlToURL(aurl_str)
	if err != nil {
		return nil, err
	}
	return h.http3Client.Get(url)
}
func (h *Host) HttpHead(aurl_str string) (*http.Response, error) {
	url, err := HackyAddressTransformAurlToURL(aurl_str)
	if err != nil {
		return nil, err
	}
	return h.http3Client.Head(url)
}
func (h *Host) HttpPost(aurl_str, contentType string, body io.Reader) (*http.Response, error) {
	url, err := HackyAddressTransformAurlToURL(aurl_str)
	if err != nil {
		return nil, err
	}
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
		NextProtos:         []string{ahmp.NextProtoAhmp, http3.NextProtoH3},
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
