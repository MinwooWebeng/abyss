package ahmp

import (
	"abyss/net/pkg/ahmp/and"
	"abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/ahmp/serializer"
	"abyss/net/pkg/aurl"
	"abyss/net/pkg/nettype"
	"reflect"
)

type ANDPeerWrapper struct {
	*pcn.Peer
}

func (p *ANDPeerWrapper) SendMessageFrameSyncMarshal(payload_type pcn.FrameType, payloads ...any) error {
	if len(payloads) == 0 {
		return p.SendMessageFrameSync(payload_type, []byte{})
	}

	conv_payloads := make([][]byte, 2*len(payloads))
	conv_off := 0
	for _, payload := range payloads {
		switch p := payload.(type) {
		case []byte:
			conv_payloads[conv_off] = nettype.MarshalQuicUint(uint64(len(p)))
			conv_off++
			conv_payloads[conv_off] = p
			conv_off++
		case string:
			conv_payloads[conv_off] = nettype.MarshalQuicUint(uint64(len(p)))
			conv_off++
			conv_payloads[conv_off] = []byte(p)
			conv_off++
		case uint64:
			conv_payloads[conv_off] = nettype.MarshalQuicUint(p)
			conv_off++
		default:
			// fmt.Println(p)
			panic("SendMessageFrameSyncMarshal: cannot marshal type " + reflect.TypeOf(p).Name())
		}
	}

	return p.SendMessageFrameSync2(payload_type, conv_payloads[:conv_off]...)
}

func (p *ANDPeerWrapper) SendJN(path string) error {
	return p.SendSerializedMessageFrameSync(pcn.JN, serializer.SerializeString(path))
}
func (p *ANDPeerWrapper) SendJOK(path string, world and.INeighborDiscoveryWorldBase, member_addrs []any) error {
	address_strings := make([]string, len(member_addrs))
	for i, addr := range member_addrs {
		address_strings[i] = addr.(*aurl.AURL).String()
	}
	return p.SendSerializedMessageFrameSync(pcn.JOK, serializer.SerializeJoinRaw([]*serializer.SerializedNode{
		serializer.SerializeString(path),
		serializer.SerializeBytes(world.GetJsonBytes()),
		serializer.SerializeStringArray(address_strings),
	}))
}
func (p *ANDPeerWrapper) SendJDN(path string, status int, message string) error {
	return p.SendMessageFrameSyncMarshal(pcn.JDN, path, uint64(status), message)
}
func (p *ANDPeerWrapper) SendJNI(world and.INeighborDiscoveryWorldBase, member and.INeighborDiscoveryPeerBase) error {
	member_url := member.GetAddress().(*aurl.AURL)
	return p.SendMessageFrameSyncMarshal(pcn.JNI, world.GetUUIDBytes(), member_url.String())
}
func (p *ANDPeerWrapper) SendMEM(world and.INeighborDiscoveryWorldBase) error {
	return p.SendMessageFrameSyncMarshal(pcn.MEM, world.GetUUIDBytes())
}
func (p *ANDPeerWrapper) SendSNB(world and.INeighborDiscoveryWorldBase, members_hash []string) error {
	//dirty code due to go's lack of variadic packing
	sugar_buf := make([]any, len(members_hash)+1)
	sugar_buf[0] = world.GetUUIDBytes()
	for i, mh := range members_hash {
		sugar_buf[i+1] = mh
	}

	return p.SendMessageFrameSyncMarshal(pcn.SNB, sugar_buf...)
}
func (p *ANDPeerWrapper) SendCRR(world and.INeighborDiscoveryWorldBase, member_hash string) error {
	return p.SendMessageFrameSyncMarshal(pcn.CRR, world.GetUUIDBytes(), member_hash)
}
func (p *ANDPeerWrapper) SendRST(world_uuid string) error {
	return p.SendMessageFrameSyncMarshal(pcn.RST, world_uuid)
}

func (p *ANDPeerWrapper) GetAddress() any {
	return p.Address
}

func (p *ANDPeerWrapper) GetHash() string {
	return p.Address.Hash
}

func PayloadPopByteSlice(payload []byte) ([]byte, bool, []byte) {
	length, uselen, ok := nettype.TryParseQuicUint(payload)
	if !ok || length+uint64(uselen) > uint64(len(payload)) {
		return nil, false, payload
	}
	return payload[uselen : uint64(uselen)+length], true, payload[uint64(uselen)+length:]
}
func PayloadPopString(payload []byte) (string, bool, []byte) {
	data, ok, rem := PayloadPopByteSlice(payload)
	if data == nil {
		return "", false, payload
	}
	return string(data), ok, rem
}
func PayloadPopUint64(payload []byte) (uint64, bool, []byte) {
	value, uselen, ok := nettype.TryParseQuicUint(payload)
	if !ok {
		return 0, false, payload
	}
	return value, true, payload[uselen:]
}
