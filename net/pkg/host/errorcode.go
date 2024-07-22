package host

import "strconv"

type PeerErrorCode int

const (
	ProtocolUnsupported = 4096 + 16*iota
)

func PeerErrorString(code int) string {
	switch code {
	case ProtocolUnsupported:
		return "ProtocolUnsupported"
	default:
		return "unknown error: " + strconv.Itoa(code)
	}
}
