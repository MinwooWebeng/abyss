package ahmp

import (
	"abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/aurl"
	"context"
	"sort"
	"strconv"
)

type handler_range struct {
	lbound  uint64
	rbound  uint64
	handler AhmpHandler
}

// TODO
type AhmpMux struct {
	handlers []handler_range
}

func NewAhmpMux() *AhmpMux {
	return &AhmpMux{
		handlers: make([]handler_range, 0),
	}
}

func (m *AhmpMux) OnConnected(ctx context.Context, peer *pcn.Peer) error {

	return nil
}
func (m *AhmpMux) OnConnectFailed(ctx context.Context, address *aurl.AURL) error {
	return nil
}
func (m *AhmpMux) ServeMessage(ctx context.Context, peer *pcn.Peer, frame *pcn.MessageFrame) error {
	return nil
}
func (m *AhmpMux) OnClosed(ctx context.Context, peer *pcn.Peer) error {
	return nil
}

// sort wrapper
type byLbound []handler_range

func (s byLbound) Len() int {
	return len(s)
}
func (s byLbound) Swap(i, j int) {
	s[i], s[j] = s[j], s[i]
}
func (s byLbound) Less(i, j int) bool {
	return s[i].lbound < s[j].lbound
}

func (m *AhmpMux) Handle(lbound, rbound uint64, handler AhmpHandler) {
	if lbound > rbound {
		panic("AhmpMux.Handle(): invalid handler range; " + strconv.Itoa(int(lbound)) + "~" + strconv.Itoa(int(rbound)))
	}

	m.handlers = append(m.handlers, handler_range{lbound: lbound, rbound: rbound, handler: handler})
	sort.Sort(byLbound(m.handlers))

	for i, h := range m.handlers {
		if i == 0 {
			continue
		}

		if m.handlers[i-1].rbound >= h.lbound {
			panic("AhmpMux.Handle(): handler range overlap; " +
				strconv.Itoa(int(m.handlers[i-1].lbound)) + "~" + strconv.Itoa(int(m.handlers[i-1].rbound)) + ", " +
				strconv.Itoa(int(h.lbound)) + "~" + strconv.Itoa(int(h.rbound)))
		}
	}
}
