package nettype

import (
	"testing"
)

func TestQuicUint(t *testing.T) {
	for _, v := range []uint64{0x3e, 0x3f, 0x41, 0x3ffe, 0x3fff, 0x4001, 0xffff, 0x3ffffffd, 0x10000, 0x10001, 0x3ffffffe, 0x3fffffff, 0x40000001, 0x3ffffffffffffffe, 0x3fffffffffffffff} {
		buf := []byte{0, 0, 0, 0, 0, 0, 0, 0, 0}
		len, ok := TryWriteQuicUint(v, buf)
		if !ok {
			t.Fatal("failed to write quic int")
		}
		if buf[len] != 0 {
			t.Fatal("write over length")
		}
		value, lenr, ok := TryParseQuicUint(buf)
		if !ok {
			t.Fatal("failed to read quic int")
		}
		if len != lenr {
			t.Fatal("length mismatch")
		}
		if v != value {
			t.Fatal("value mismatch from ", v, value, buf)
		}
	}
}

func TestQuicUintOverflow(t *testing.T) {
	for _, v := range []uint64{0x4000000000000000, 0x4000000000000001} {
		buf := []byte{0, 0, 0, 0, 0, 0, 0, 0, 0}
		_, ok := TryWriteQuicUint(v, buf)
		if ok {
			t.Fatal("failed to detect overflow")
		}
	}
}
