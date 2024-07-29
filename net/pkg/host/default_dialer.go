package host

import (
	"abyss/net/pkg/aurl"
	"context"
	"crypto/tls"
	"errors"
	"net"
	"net/netip"
	"sync/atomic"

	"github.com/quic-go/quic-go"
)

type DefaultDialer struct {
	DialContext context.Context
	Transport   *quic.Transport
	TlsConf     *tls.Config
	QuicConf    *quic.Config
	LocalAddr   atomic.Value //must be *aurl.AURL
}

func NewDefaultDialer(ctx context.Context, tr *quic.Transport, tlsConf *tls.Config, quicConf *quic.Config, local_hash string) *DefaultDialer {
	result := &DefaultDialer{
		DialContext: ctx,
		Transport:   tr,
		TlsConf:     tlsConf,
		QuicConf:    quicConf,
	}
	local_addr, _ := result.Transport.Conn.LocalAddr().(*net.UDPAddr)
	local_ip, _ := netip.ParseAddr("127.0.0.1")
	result.LocalAddr.Store(&aurl.AURL{
		Hash:       local_hash,
		Candidates: []*net.UDPAddr{net.UDPAddrFromAddrPort(netip.AddrPortFrom(local_ip, uint16(local_addr.Port)))},
		Path:       "/",
	})
	return result
}

func IpExist(list []*net.UDPAddr, subject net.IP) bool {
	for _, addr := range list {
		if addr.IP.Equal(subject) {
			return true
		}
	}
	return false
}

func (d *DefaultDialer) Dial(remote_addr *aurl.AURL) (quic.Connection, error) {
	if len(remote_addr.Candidates) == 0 {
		return nil, errors.New("Dial: no remote address candidates")
	}

	local_addr, ok := d.LocalAddr.Load().(*aurl.AURL)
	if !ok {
		panic("Dial: LocalAddr type assertion fail")
	}

	for _, candidate := range remote_addr.Candidates {
		if !IpExist(local_addr.Candidates, candidate.IP) {
			return d.Transport.Dial(d.DialContext, candidate, d.TlsConf, d.QuicConf)
		}
	}

	//all ip address overlap.
	last_resort := remote_addr.Candidates[len(remote_addr.Candidates)-1]
	local_loopback := local_addr.Candidates[len(local_addr.Candidates)-1]
	if last_resort.IP.Equal(local_loopback.IP) && last_resort.Port != local_loopback.Port {
		return d.Transport.Dial(d.DialContext, last_resort, d.TlsConf, d.QuicConf)
	}
	return nil, errors.New("Dial: same endpoint")
}

func (d *DefaultDialer) LocalAddress() *aurl.AURL {
	return d.LocalAddr.Load().(*aurl.AURL)
}
