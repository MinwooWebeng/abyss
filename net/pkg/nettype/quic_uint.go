package nettype

import (
	"encoding/binary"
	"io"
)

func TryReadQuicUint(reader io.Reader) (uint64, error) {
	buf := [8]byte{}
	var err error
	if _, err = reader.Read(buf[:1]); err != nil {
		return 0, err
	}

	switch buf[0] & 0b11000000 {
	case 0b00000000:
		return uint64(buf[0]) & 0x3f, nil
	case 0b01000000:
		if _, err = reader.Read(buf[1:2]); err == nil {
			return uint64(binary.BigEndian.Uint16(buf[:]) & 0x3fff), nil
		}
	case 0b10000000:
		if _, err = io.ReadFull(reader, buf[1:4]); err == nil {
			return uint64(binary.BigEndian.Uint32(buf[:]) & 0x3fffffff), nil
		}
	case 0b11000000:
		if _, err = io.ReadFull(reader, buf[1:8]); err == nil {
			return uint64(binary.BigEndian.Uint64(buf[:]) & 0x3fffffffffffffff), nil
		}
	default:
		panic("QuicUintLen: bitmask fail")
	}
	return 0, err
}

func TryParseQuicUint(data []byte) (uint64, int, bool) { //value, used length, ok
	if len(data) == 0 {
		return 0, 0, false
	}
	switch data[0] & 0b11000000 {
	case 0b00000000:
		return uint64(data[0]) & 0x3f, 1, true
	case 0b01000000:
		if len(data) < 2 {
			return 0, 0, false
		}
		return uint64(binary.BigEndian.Uint16(data)) & 0x3fff, 2, true
	case 0b10000000:
		if len(data) < 4 {
			return 0, 0, false
		}
		return uint64(binary.BigEndian.Uint32(data)) & 0x3fffffff, 4, true
	case 0b11000000:
		if len(data) < 8 {
			return 0, 0, false
		}
		return binary.BigEndian.Uint64(data) & 0x3fffffffffffffff, 8, true
	}
	return 0, 0, false
}

func TryWriteQuicUint(value uint64, buf []byte) (int, bool) { //used length, ok
	if value <= 0x3f {
		if len(buf) == 0 {
			return 0, false
		}
		buf[0] = byte(uint8(value))
		return 1, true
	}
	if value <= 0x3fff {
		if len(buf) < 2 {
			return 0, false
		}
		binary.BigEndian.PutUint16(buf, uint16(value))
		buf[0] = buf[0] | 0b01000000
		return 2, true
	}
	if value <= 0x3fffffff {
		if len(buf) < 4 {
			return 0, false
		}
		binary.BigEndian.PutUint32(buf, uint32(value))
		buf[0] = buf[0] | 0b10000000
		return 4, true
	}
	if value <= 0x3fffffffffffffff {
		if len(buf) < 8 {
			return 0, false
		}
		binary.BigEndian.PutUint64(buf, value)
		buf[0] = buf[0] | 0b11000000
		return 8, true
	}
	return 0, false
}
