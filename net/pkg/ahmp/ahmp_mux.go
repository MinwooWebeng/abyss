package ahmp

import (
	"abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/aurl"
	"abyss/net/pkg/functional"
	"abyss/net/pkg/generr"
	"context"
	"errors"
	"reflect"
	"sort"
	"strconv"
)

type handler_range struct {
	lbound       uint64
	rbound       uint64
	handler      AhmpHandler
	handler_type reflect.Type
}

type AhmpMuxHandlerError struct {
	HandlerType reflect.Type
	Err         error
}

func (e *AhmpMuxHandlerError) Error() string {
	return e.HandlerType.Name() + ": " + e.Err.Error()
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
	handler_results := functional.Filter_ok(m.handlers, func(handler handler_range) (*AhmpMuxHandlerError, bool) {
		err := handler.handler.OnConnected(ctx, peer)
		if err != nil {
			return &AhmpMuxHandlerError{handler.handler_type, err}, true
		}
		return nil, false
	})
	if len(handler_results) != 0 {
		return &generr.MultiError[*AhmpMuxHandlerError]{Errors: handler_results}
	}
	return nil
}
func (m *AhmpMux) OnConnectFailed(ctx context.Context, address *aurl.AURL) error {
	handler_results := functional.Filter_ok(m.handlers, func(handler handler_range) (*AhmpMuxHandlerError, bool) {
		err := handler.handler.OnConnectFailed(ctx, address)
		if err != nil {
			return &AhmpMuxHandlerError{handler.handler_type, err}, true
		}
		return nil, false
	})
	if len(handler_results) != 0 {
		return &generr.MultiError[*AhmpMuxHandlerError]{Errors: handler_results}
	}
	return nil
}
func (m *AhmpMux) ServeMessage(ctx context.Context, peer *pcn.Peer, frame *pcn.MessageFrame) error {
	index, _ := sort.Find(len(m.handlers), func(i int) int {
		//return smallest index i that cmp(i) <= 0. cmp(i) must monotonically decrease with i.
		return int(uint64(frame.Type) - m.handlers[i].rbound) //rbound monotonically increases.
	})
	target_handler := m.handlers[index]
	if uint64(frame.Type) < target_handler.lbound {
		return errors.New("unhandled frame(" + frame.Type.String() + ")")
	}
	return target_handler.handler.ServeMessage(ctx, peer, frame)
}
func (m *AhmpMux) OnClosed(ctx context.Context, peer *pcn.Peer) error {
	handler_results := functional.Filter_ok(m.handlers, func(handler handler_range) (*AhmpMuxHandlerError, bool) {
		err := handler.handler.OnClosed(ctx, peer)
		if err != nil {
			return &AhmpMuxHandlerError{handler.handler_type, err}, true
		}
		return nil, false
	})
	if len(handler_results) != 0 {
		return &generr.MultiError[*AhmpMuxHandlerError]{Errors: handler_results}
	}
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

	m.handlers = append(m.handlers, handler_range{lbound: lbound, rbound: rbound, handler: handler, handler_type: reflect.TypeOf(handler)})
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
