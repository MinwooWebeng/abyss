package ahmp

import (
	"abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/aurl"
	"context"
	"encoding/binary"
	"errors"
	"math/rand/v2"
	"sync"
	"time"
)

type HangingPingRet struct {
	start_time time.Time
	ret_ch     chan time.Duration
}

type PingHandler struct {
	mtx sync.Mutex

	hangings map[uint32]HangingPingRet
}

func NewPingHandler() *PingHandler {
	return &PingHandler{
		hangings: make(map[uint32]HangingPingRet),
	}
}

func (h *PingHandler) PingRTT(peer *pcn.Peer) <-chan time.Duration {
	ping_id := uint32(rand.IntN(4294967295))

	bs := make([]byte, 4)
	binary.LittleEndian.PutUint32(bs, ping_id)
	ret_ch := make(chan time.Duration)

	h.mtx.Lock()
	defer h.mtx.Unlock()

	h.hangings[ping_id] = HangingPingRet{time.Now(), ret_ch}
	peer.SendMessageFrameSync(pcn.PINGT, bs)
	// fmt.Println("sent ping: ", ping_id)
	return ret_ch
}

func (h *PingHandler) OnConnected(ctx context.Context, peer *pcn.Peer) error {
	return nil
}
func (h *PingHandler) OnConnectFailed(ctx context.Context, address *aurl.AURL) error {
	return nil
}
func (h *PingHandler) OnClosed(ctx context.Context, peer *pcn.Peer) error {
	return nil
}

func (h *PingHandler) ServeMessage(ctx context.Context, peer *pcn.Peer, frame *pcn.MessageFrame) error {
	// fmt.Println("PingHandler: serve", frame)
	switch frame.Type {
	case pcn.PINGT:
		// fmt.Println("echo!")
		peer.SendMessageFrameSync(pcn.PINGR, frame.Payload)
	case pcn.PINGR:
		ping_id := binary.LittleEndian.Uint32(frame.Payload)

		h.mtx.Lock()
		defer h.mtx.Unlock()

		hanging_call, ok := h.hangings[ping_id]
		if !ok {
			return errors.New("PingHandler: unexpected ping return")
		}
		delete(h.hangings, ping_id)
		hanging_call.ret_ch <- time.Since(hanging_call.start_time)
	}
	return nil
}
