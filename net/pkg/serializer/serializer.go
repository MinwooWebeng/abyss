package serializer

import (
	"abyss/net/pkg/functional"
	"abyss/net/pkg/nettype"
)

type SerializedNode struct {
	Size int
	Body []byte
	Leaf []*SerializedNode
}

func SerializeRawBytes(data []byte) *SerializedNode {
	return &SerializedNode{
		Size: len(data),
		Body: data,
		Leaf: nil,
	}
}

func SerializeBytes(data []byte) *SerializedNode {
	data_leaf := SerializeRawBytes(data)
	length_serialized := nettype.MarshalQuicUint(uint64(data_leaf.Size))
	return &SerializedNode{
		Size: len(length_serialized) + data_leaf.Size,
		Body: length_serialized,
		Leaf: []*SerializedNode{data_leaf},
	}
}

func SerializeString(data string) *SerializedNode {
	return SerializeBytes([]byte(data))
}

func SerializeUInt(data uint64) *SerializedNode {
	data_serialized := nettype.MarshalQuicUint(data)
	return &SerializedNode{
		Size: len(data_serialized),
		Body: data_serialized,
		Leaf: nil,
	}
}

func SerializeUIntArray(data []uint64) *SerializedNode {
	count_serialized := nettype.MarshalQuicUint(uint64(len(data)))
	data_serialized := make([]*SerializedNode, len(data))
	size := len(count_serialized)
	for i, d := range data {
		d_s := SerializeUInt(d)
		data_serialized[i] = d_s
		size += d_s.Size
	}
	return &SerializedNode{
		Size: size,
		Body: count_serialized,
		Leaf: data_serialized,
	}
}

func SerializeStringArray(data []string) *SerializedNode {
	count_serialized := nettype.MarshalQuicUint(uint64(len(data)))
	data_serialized := make([]*SerializedNode, len(data))
	size := len(count_serialized)
	for i, d := range data {
		d_s := SerializeString(d)
		data_serialized[i] = d_s
		size += d_s.Size
	}
	return &SerializedNode{
		Size: size,
		Body: count_serialized,
		Leaf: data_serialized,
	}
}

func SerializeBytesArray(data [][]byte) *SerializedNode {
	count_serialized := nettype.MarshalQuicUint(uint64(len(data)))
	data_serialized := make([]*SerializedNode, len(data))
	size := len(count_serialized)
	for i, d := range data {
		d_s := SerializeBytes(d)
		data_serialized[i] = d_s
		size += d_s.Size
	}
	return &SerializedNode{
		Size: size,
		Body: count_serialized,
		Leaf: data_serialized,
	}
}

func SerializeJoinRaw(serialized []*SerializedNode) *SerializedNode {
	size := 0
	for _, s := range serialized {
		size += s.Size
	}
	return &SerializedNode{
		Size: size,
		Body: nil,
		Leaf: serialized,
	}
}

func SerializeJoinWithCount(serialized []*SerializedNode) *SerializedNode {
	count_serialized := nettype.MarshalQuicUint(uint64(len(serialized)))
	size := len(count_serialized)
	for _, s := range serialized {
		size += s.Size
	}
	return &SerializedNode{
		Size: size,
		Body: count_serialized,
		Leaf: serialized,
	}
}

func SerializeJoinWithSize(serialized []*SerializedNode) *SerializedNode {
	leaf_size_sum := 0
	for _, s := range serialized {
		leaf_size_sum += s.Size
	}
	leaf_size_serialized := nettype.MarshalQuicUint(uint64(leaf_size_sum))
	return &SerializedNode{
		Size: len(leaf_size_serialized) + leaf_size_sum,
		Body: leaf_size_serialized,
		Leaf: serialized,
	}
}

func SerializeObjectArray[T any](objects []T, serialize_func func(T) []*SerializedNode) *SerializedNode {
	return SerializeJoinWithCount(
		functional.Filter(objects, func(o T) *SerializedNode {
			return SerializeJoinWithSize(serialize_func(o))
		}),
	)
}

func SerializeEmptyArray() *SerializedNode {
	return SerializeUInt(0)
}
