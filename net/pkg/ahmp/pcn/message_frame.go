package pcn

import (
	"abyss/net/pkg/nettype"
	"errors"
	"io"
)

type MessageFrame struct {
	ContentLength uint64
	Type          FrameType
	Payload       []byte
}

func ReceiveMessageFrame(reader io.Reader) (*MessageFrame, error) {
	m := &MessageFrame{}

	content_len, err := nettype.TryReadQuicUint(reader)
	if err != nil {
		return nil, err
	}
	m.ContentLength = content_len

	type_id, err := nettype.TryReadQuicUint(reader)
	if err != nil {
		return nil, err
	}
	m.Type = FrameType(type_id)

	m.Payload = make([]byte, m.ContentLength)
	if _, err := io.ReadFull(reader, m.Payload); err != nil {
		return nil, err
	}

	return m, nil
}

// ***Not Thread Safe*** never use this from peer. use peer.SendMessageFrameSync() for thread safety.
func SendMessageFrame(writer io.Writer, payload []byte, payload_type FrameType) error {
	buf := [16]byte{}
	cl_len, ok := nettype.TryWriteQuicUint(uint64(len(payload)), buf[:])
	if !ok {
		return errors.New("AHMP Frame: Content-Length encoding fail")
	}
	ty_len, ok := nettype.TryWriteQuicUint(uint64(payload_type), buf[cl_len:])
	if !ok {
		return errors.New("AHMP Frame: Type encoding fail")
	}
	if _, err := writer.Write(buf[:cl_len+ty_len]); err != nil {
		return err
	}
	if _, err := writer.Write(payload); err != nil {
		return err
	}
	return nil
}
