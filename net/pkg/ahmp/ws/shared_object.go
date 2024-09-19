package ws

import (
	"abyss/net/pkg/serializer"

	"github.com/google/uuid"
)

type SharedObject struct {
	URL             string
	UUID            uuid.UUID //current object instance
	InitialPosition string
}

func SerializeSharedObject(s *SharedObject) []*serializer.SerializedNode {
	return []*serializer.SerializedNode{
		serializer.SerializeString(s.URL),
		serializer.SerializeString(s.UUID.String()),
		serializer.SerializeString(s.InitialPosition),
	}
}

func DeserialzeSharedObject(data []byte) (*SharedObject, int, bool) {
	url, ulen1, ok := serializer.DeserializeString(data)
	if !ok {
		return nil, 0, false
	}
	data_rem := data[ulen1:]

	uuid_str, ulen2, ok := serializer.DeserializeString(data_rem)
	if !ok {
		return nil, 0, false
	}
	uuid_parsed, err := uuid.Parse(uuid_str)
	if err != nil {
		return nil, 0, false
	}
	data_rem = data_rem[ulen2:]

	initial_pos_str, _, ok := serializer.DeserializeString(data_rem)
	if !ok {
		return nil, 0, false
	}

	return &SharedObject{url, uuid_parsed, initial_pos_str}, ulen1 + ulen2, true
}
