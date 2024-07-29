package pcn

import (
	"bytes"
	"testing"
)

func Test_MessageFrameRW(t *testing.T) {
	buf := new(bytes.Buffer)

	if err := SendMessageFrame(buf, []byte("mallang string is here"), 282725); err != nil {
		t.Fatal(err.Error())
	}
	msg, err := ReceiveMessageFrame(buf)
	if err != nil {
		t.Fatal(err.Error())
	}
	if string(msg.Payload) != "mallang string is here" {
		t.Fatal("payload mismatch: ", string(msg.Payload))
	}
	if msg.Type != 282725 {
		t.Fatal("type mismatch: ", msg.Type)
	}
}
