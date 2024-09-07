package ahmp

import (
	"abyss/net/pkg/ahmp/and"
	"abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/aurl"
	"abyss/net/pkg/serializer"
	"context"
	"encoding/json"
	"errors"
	"sync"
	"time"

	"github.com/google/uuid"
)

type ANDWorld struct {
	UUID string
	URL  string

	//inner
	json_bytes []byte
}

func NewANDWorld(url string) *ANDWorld {
	result := &ANDWorld{
		UUID: uuid.New().String(),
		URL:  url,
	}
	result.json_bytes, _ = json.Marshal(result)
	return result
}

func ParseANDWorldJson(json_bytes []byte) (*ANDWorld, error) {
	result := &ANDWorld{
		json_bytes: json_bytes,
	}
	if err := json.Unmarshal(json_bytes, result); err != nil {
		return nil, err
	}
	return result, nil
}

func (w *ANDWorld) GetJsonBytes() []byte {
	return w.json_bytes
}
func (w *ANDWorld) GetUUID() string {
	return w.UUID
}
func (w *ANDWorld) GetUUIDBytes() []byte {
	return []byte(w.UUID)
}

type ANDHandler struct {
	algorithm *and.NeighborDiscoveryHandler
	mtx       sync.Mutex
}

func NewANDHandler(ctx context.Context, local_hash string, listener chan<- and.NeighborDiscoveryEvent, on_join_callback func(string, string)) *ANDHandler {
	result := &ANDHandler{
		algorithm: and.NewNeighborDiscoveryHandler(local_hash, on_join_callback),
	}
	result.algorithm.ReserveEventListener(listener)
	result.algorithm.ReserveSNBTimer(func(duration time.Duration, wuid string) {
		go func() {
			select {
			case <-time.NewTimer(duration).C:
				result.algorithm.OnSNBTimeout(wuid)
			case <-ctx.Done():
			}
		}()
	})
	return result
}

func (m *ANDHandler) ReserveConnectCallback(callback func(*aurl.AURL)) {
	m.algorithm.ReserveConnectCallback(func(address any) {
		aurl := address.(*aurl.AURL)
		callback(aurl)
	})
}

func (m *ANDHandler) OnConnected(ctx context.Context, peer *pcn.Peer) error {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	return m.algorithm.Connected(&ANDPeerWrapper{peer})
}
func (m *ANDHandler) OnConnectFailed(ctx context.Context, address *aurl.AURL) error {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	return m.algorithm.Disconnected(address.Hash)
}

func (m *ANDHandler) ServeMessage(ctx context.Context, peer *pcn.Peer, frame *pcn.MessageFrame) error {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	switch frame.Type { //TODO: parse message and synchronize between calls.
	case pcn.JN:
		deserialized, _, ok := serializer.DeserializeString(frame.Payload)
		if !ok {
			return errors.New("JN: failed to parse")
		}
		return m.algorithm.OnJN(&ANDPeerWrapper{peer}, deserialized)
	case pcn.JOK:
		path, ulen1, ok := serializer.DeserializeString(frame.Payload)
		if !ok {
			return errors.New("JOK: failed to parse path")
		}
		payload_rem := frame.Payload[ulen1:]
		world_json, ulen2, ok := serializer.DeserializeBytes(payload_rem)
		if !ok {
			return errors.New("JOK: failed to parse json")
		}
		payload_rem = payload_rem[ulen2:]

		world, err := ParseANDWorldJson(world_json)
		if err != nil {
			return errors.New("JOK: failed to parse json")
		}

		member_addrs, _, ok := serializer.DeserializeStringArray(payload_rem)
		if !ok {
			return errors.New("JOK: failed to parse member addresses")
		}
		member_addrs_any := make([]any, len(member_addrs))
		for i, a := range member_addrs {
			member_addrs_any[i], err = aurl.ParseAURL(a)
			if err != nil {
				return errors.New("JOK: failed to parse member addresses(" + a + ")")
			}
		}
		return m.algorithm.OnJOK(&ANDPeerWrapper{peer}, path, world, member_addrs_any)
	case pcn.JDN:
		path, ok, rem := PayloadPopString(frame.Payload)
		if !ok {
			peer.CloseWithError(errors.New("JDN: failed to parse"))
			return errors.New("JDN: failed to parse")
		}
		status, ok, rem := PayloadPopUint64(rem)
		if !ok {
			peer.CloseWithError(errors.New("JDN: failed to parse"))
			return errors.New("JDN: failed to parse")
		}
		message, ok, _ := PayloadPopString(rem)
		if !ok {
			peer.CloseWithError(errors.New("JDN: failed to parse"))
			return errors.New("JDN: failed to parse")
		}
		return m.algorithm.OnJDN(&ANDPeerWrapper{peer}, path, int(status), message)
	case pcn.JNI:
		wuid, ok, rem := PayloadPopString(frame.Payload)
		if !ok {
			peer.CloseWithError(errors.New("JNI: failed to parse 1"))
			return errors.New("JNI: failed to parse 1")
		}
		aurl_str, ok, _ := PayloadPopString(rem)
		if !ok {
			peer.CloseWithError(errors.New("JNI: failed to parse 2"))
			return errors.New("JNI: failed to parse 2")
		}
		aurl, err := aurl.ParseAURL(aurl_str)
		if err != nil {
			peer.CloseWithError(errors.New("JNI: failed to parse aurl"))
			return errors.New("JNI: failed to parse aurl")
		}
		return m.algorithm.OnJNI(&ANDPeerWrapper{peer}, wuid, aurl, aurl.Hash)
	case pcn.MEM:
		wuid, ok, _ := PayloadPopString(frame.Payload)
		if !ok {
			peer.CloseWithError(errors.New("MEM: failed to parse"))
			return errors.New("MEM: failed to parse")
		}
		return m.algorithm.OnMEM(&ANDPeerWrapper{peer}, wuid)
	case pcn.SNB:
		wuid, ok, rem := PayloadPopString(frame.Payload)
		if !ok {
			peer.CloseWithError(errors.New("SNB: failed to parse"))
			return errors.New("SNB: failed to parse")
		}
		members := []string{}
		for {
			var member string
			member, ok, rem = PayloadPopString(rem)
			if !ok {
				break
			}
			members = append(members, member)
		}
		return m.algorithm.OnSNB(&ANDPeerWrapper{peer}, wuid, members)
	case pcn.CRR:
		wuid, ok, rem := PayloadPopString(frame.Payload)
		if !ok {
			peer.CloseWithError(errors.New("CRR: failed to parse"))
			return errors.New("CRR: failed to parse")
		}
		member, ok, _ := PayloadPopString(rem)
		if !ok {
			peer.CloseWithError(errors.New("CRR: failed to parse"))
			return errors.New("CRR: failed to parse")
		}
		// fmt.Println("CRR:", member)
		return m.algorithm.OnCRR(&ANDPeerWrapper{peer}, wuid, member)
	case pcn.RST:
		wuid, ok, _ := PayloadPopString(frame.Payload)
		if !ok {
			peer.CloseWithError(errors.New("RST: failed to parse"))
			return errors.New("RST: failed to parse")
		}
		return m.algorithm.OnRST(&ANDPeerWrapper{peer}, wuid)
	}
	return errors.New("ahmp message unhandled")
}
func (m *ANDHandler) OnClosed(ctx context.Context, peer *pcn.Peer) error {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	return m.algorithm.Disconnected(peer.Address.Hash)
}

func (m *ANDHandler) OpenWorld(path string, world *ANDWorld) error {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	return m.algorithm.OpenWorld(path, world)
}
func (m *ANDHandler) CloseWorld(path string) {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	m.algorithm.CloseWorld(path)
}
func (m *ANDHandler) ChangeWorldPath(prev_path string, new_path string) bool {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	return m.algorithm.ChangeWorldPath(prev_path, new_path)
}
func (m *ANDHandler) GetWorld(path string) (*ANDWorld, bool) {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	p, ok := m.algorithm.GetWorld(path)
	if ok {
		return p.(*ANDWorld), true
	}
	return nil, false
}

func (m *ANDHandler) JoinConnected(local_path string, peer *pcn.Peer, path string) error {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	return m.algorithm.JoinConnected(local_path, &ANDPeerWrapper{peer}, path)
}

func (m *ANDHandler) JoinAny(local_path string, aurl *aurl.AURL) error {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	if aurl.Scheme != "abyss" {
		return errors.New("url scheme mismatch")
	}

	return m.algorithm.JoinAny(local_path, aurl, aurl.Hash, aurl.Path)
}
