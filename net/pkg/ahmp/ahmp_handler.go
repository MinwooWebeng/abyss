package ahmp

import (
	"abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/aurl"
	"abyss/net/pkg/functional"
	"context"
	"strings"
)

type AhmpHandleEventType int

const (
	Invalid AhmpHandleEventType = iota
	OnConnected
	OnConnectFailed
	ServeMessage
	OnClosed
)

type AhmpHandler interface {
	OnConnected(ctx context.Context, peer *pcn.Peer) *AhmpHandlerError
	OnConnectFailed(ctx context.Context, address *aurl.AURL) *AhmpHandlerError
	ServeMessage(ctx context.Context, peer *pcn.Peer, frame *pcn.MessageFrame) *AhmpHandlerError
	OnClosed(ctx context.Context, peer *pcn.Peer) *AhmpHandlerError
}

//TODO: define interface for partial handler; only ServeMessage/ServeMessage+OnClosed/etc.
//and make AhmpHandler as composite interface.

type AhmpHandlerError struct {
	EventType AhmpHandleEventType
	PeerHash  string
	Errs      []error //this must not contain *AhmpHandlerError
}

func NewAhmpHandlerError(event_type AhmpHandleEventType, peer_hash string, errs []error) *AhmpHandlerError {
	conjugated_errs := make([]error, 0, functional.Accum_all(errs, 0, func(err error, count int) int {
		handler_err, ok := err.(*AhmpHandlerError)
		if ok {
			return count + len(handler_err.Errs)
		}
		return count + 1
	}))

	functional.Accum_all(errs, conjugated_errs, func(err error, result []error) []error {
		handler_err, ok := err.(*AhmpHandlerError)
		if ok {
			return append(result, handler_err.Errs...)
		}
		return append(result, err)
	})

	return &AhmpHandlerError{
		EventType: event_type,
		PeerHash:  peer_hash,
		Errs:      conjugated_errs,
	}
}

func (e *AhmpHandlerError) Error() string {
	switch e.EventType {
	case OnConnected:
		return "OnConnected(" + e.PeerHash + ") [" +
			strings.Join(functional.Filter(e.Errs, func(e error) string { return e.Error() }), ", ") + "]"
	case OnConnectFailed:
		return "OnConnectFailed(" + e.PeerHash + ") [" +
			strings.Join(functional.Filter(e.Errs, func(e error) string { return e.Error() }), ", ") + "]"
	case ServeMessage:
		return "ServeMessage(" + e.PeerHash + ") [" +
			strings.Join(functional.Filter(e.Errs, func(e error) string { return e.Error() }), ", ") + "]"
	case OnClosed:
		return "OnClosed(" + e.PeerHash + ") [" +
			strings.Join(functional.Filter(e.Errs, func(e error) string { return e.Error() }), ", ") + "]"
	}

	panic("AhmpHandlerError: undefined error type")
}
