package serializer

import (
	"abyss/net/pkg/functional"
	"abyss/net/pkg/nettype"
)

func DeserializeBytes(data []byte) ([]byte, int, bool) {
	val, usedlen, ok := nettype.TryParseQuicUint(data)
	if !ok || int(val) > len(data)-usedlen {
		return nil, 0, false
	}
	return data[usedlen : usedlen+int(val)], usedlen + int(val), true
}

func DeserializeString(data []byte) (string, int, bool) {
	val, usedlen, ok := DeserializeBytes(data)
	return string(val), usedlen, ok
}

func DeserializeUInt(data []byte) (uint64, int, bool) {
	return nettype.TryParseQuicUint(data)
}

func DeserializeUIntArray(data []byte) ([]uint64, int, bool) {
	val, usedlen, ok := nettype.TryParseQuicUint(data)
	if !ok {
		return nil, 0, false
	}
	body := data[usedlen:]
	result := make([]uint64, val)
	for i := range val {
		i_val, i_usedlen, i_ok := nettype.TryParseQuicUint(body)
		if !i_ok {
			return nil, 0, false
		}
		result[i] = i_val
		body = body[i_usedlen:]
		usedlen += i_usedlen
	}
	return result, usedlen, true
}

func DeserializeStringArray(data []byte) ([]string, int, bool) {
	val, usedlen, ok := nettype.TryParseQuicUint(data)
	if !ok {
		return nil, 0, false
	}
	body := data[usedlen:]
	result := make([]string, val)
	for i := range val {
		i_val, i_usedlen, i_ok := DeserializeString(body)
		if !i_ok {
			return nil, 0, false
		}
		result[i] = i_val
		body = body[i_usedlen:]
		usedlen += i_usedlen
	}
	return result, usedlen, true
}

func DeserializeBytesArray(data []byte) ([][]byte, int, bool) {
	val, usedlen, ok := nettype.TryParseQuicUint(data)
	if !ok {
		return nil, 0, false
	}
	body := data[usedlen:]
	result := make([][]byte, val)
	for i := range val {
		i_val, i_usedlen, i_ok := DeserializeBytes(body)
		if !i_ok {
			return nil, 0, false
		}
		result[i] = i_val
		body = body[i_usedlen:]
		usedlen += i_usedlen
	}
	return result, usedlen, true
}

func DeserializeObjectArray[T any](data []byte, deserialize_func func([]byte) (T, int, bool)) ([]T, int, bool) {
	split, ulen, ok := DeserializeBytesArray(data)
	if !ok {
		return nil, 0, false
	}
	result, ok := functional.Filter_strict_ok(
		split,
		func(data []byte) (T, bool) { v, _, ok := deserialize_func(data); return v, ok },
	)
	return result, ulen, ok
}

func LegacyDes[R any](f func([]byte) (R, int, bool)) func([]byte) (R, []byte, bool) {
	return func(data []byte) (R, []byte, bool) {
		res, ulen, ok := f(data)
		return res, data[ulen:], ok
	}
}

func LegacyDesObjectArray[R any](desfunc func([]byte) (R, int, bool)) func([]byte) ([]R, []byte, bool) {
	return func(data []byte) ([]R, []byte, bool) {
		res, ulen, ok := DeserializeObjectArray(data, desfunc)
		return res, data[ulen:], ok
	}
}
