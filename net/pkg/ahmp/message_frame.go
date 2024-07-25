package ahmp

import (
	"abyss/net/pkg/nettype"
	"errors"
	"io"
)

type MessageFrame struct {
	ContentLength uint64
	Type          uint64
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
	m.Type = type_id

	m.Payload = make([]byte, m.ContentLength)
	if _, err := io.ReadFull(reader, m.Payload); err != nil {
		return nil, err
	}

	return m, nil
}

func SendMessageFrame(writer io.Writer, payload []byte, payload_type uint64) error {
	buf := [16]byte{}
	cl_len, ok := nettype.TryWriteQuicUint(uint64(len(payload)), buf[:])
	if !ok {
		return errors.New("AHMP Frame: Content-Length encoding fail")
	}
	ty_len, ok := nettype.TryWriteQuicUint(payload_type, buf[cl_len:])
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
