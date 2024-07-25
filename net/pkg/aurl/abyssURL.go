package aurl

import (
	"net"
	"strconv"
	"strings"
)

type AbyssURL struct {
	Hash string
	Addr net.Addr
	Path string
}

func (a *AbyssURL) String() string {
	return "abyss:" + a.Hash + ":" + a.Addr.String()
}

type AURLParseError struct {
	Code int
}

func (u *AURLParseError) Error() string {
	var msg string
	switch u.Code {
	case 100:
		msg = "invalid format"
	case 101:
		msg = "unsupported protocol"
	case 102:
		msg = "hash too short"
	case 103:
		msg = "ip:port parse fail"
	default:
		msg = "unknown error (" + strconv.Itoa(u.Code) + ")"
	}
	return "failed to parse abyssURL: " + msg
}

func ParseAbyssURL(raw string) (*AbyssURL, error) {
	split := strings.SplitN(raw, ":", 3)
	if len(split) != 3 {
		return nil, &AURLParseError{Code: 100}
	}

	if split[0] != "abyss" {
		return nil, &AURLParseError{Code: 101}
	}

	if len(split[1]) < 32 {
		return nil, &AURLParseError{Code: 102}
	}

	split2 := strings.SplitN(split[2], "/", 2)
	addr, err := net.ResolveUDPAddr("udp", split2[0])
	if err != nil {
		return nil, &AURLParseError{Code: 103}
	}

	path := ""
	if len(split) == 2 {
		path = "/" + split2[1]
	}

	return &AbyssURL{
		Hash: split[1],
		Addr: addr,
		Path: path,
	}, nil
}
