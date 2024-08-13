package serializer

import (
	"bytes"
	"io"
	"testing"
)

func writeSerializedToBuffer(buffer io.Writer, serialized *SerializedNode) {
	if serialized.Body != nil {
		buffer.Write(serialized.Body)
	}
	if serialized.Leaf != nil {
		for _, l := range serialized.Leaf {
			writeSerializedToBuffer(buffer, l)
		}
	}
}

func SerializedToBytes(serialized *SerializedNode) []byte {
	b := bytes.NewBuffer(make([]byte, 0, serialized.Size))
	writeSerializedToBuffer(b, serialized)
	return b.Bytes()
}

func TestSimple(t *testing.T) {
	{
		o := SerializedToBytes(SerializeUInt(7232))
		u, ul, ok := DeserializeUInt(o)
		if !ok {
			t.Fatal()
		}
		if ul != len(o) {
			t.Fatal()
		}
		if u != 7232 {
			t.Fatal()
		}
	}
	{
		o := SerializedToBytes(SerializeString("mallang world!"))
		u, ul, ok := DeserializeString(o)
		if !ok {
			t.Fatal()
		}
		if ul != len(o) {
			t.Fatal()
		}
		if u != "mallang world!" {
			t.Fatal()
		}
	}
	{
		raw := []string{"mallang world!", "carrot!", "", "cute"}
		o := SerializedToBytes(SerializeStringArray(raw))
		u, ul, ok := DeserializeStringArray(o)
		if !ok {
			t.Fatal()
		}
		if ul != len(o) {
			t.Fatal()
		}
		for i, e := range u {
			if e != raw[i] {
				t.Fatal()
			}
		}
	}
}

func TestAdvanced(t *testing.T) {
	{
		raw := []string{"mallang world!", "carrot!", "", "cute"}
		o := SerializedToBytes(SerializeJoinRaw([]*SerializedNode{
			SerializeStringArray(raw),
			SerializeUInt(6253),
		}))
		u1, ul1, ok1 := DeserializeStringArray(o)
		if !ok1 {
			t.Fatal()
		}
		for i, e := range u1 {
			if e != raw[i] {
				t.Fatal()
			}
		}
		u2, ul2, ok2 := DeserializeUInt(o[ul1:])
		if !ok2 {
			t.Fatal()
		}
		if ul1+ul2 != len(o) {
			t.Fatal()
		}
		if u2 != 6253 {
			t.Fatal()
		}
	}
}

//TODO: test for malicious cases
