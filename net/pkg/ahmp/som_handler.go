package ahmp

import (
	advancedmap "abyss/net/pkg/advanced_map"
	"abyss/net/pkg/ahmp/pcn"
	"abyss/net/pkg/ahmp/ws"
	"abyss/net/pkg/aurl"
	"abyss/net/pkg/functional"
	"abyss/net/pkg/serializer"
	"context"
	"errors"
	"sync"
)

type SomEventType int

const (
	SomReNew SomEventType = iota
	SomAppend
	SomDelete
)

type SomEvent struct {
	Type        SomEventType
	PeerHash    string
	WorldUUID   string
	SomObjects  []*ws.SharedObject //SO, SOA
	SomObjUUIDs []string           //SOD
}

type SOMServeObjectGroup struct {
	isActive bool               //this is set to true when the host receives SOR. Only active group sends SO/SOA/SOD
	objects  []*ws.SharedObject //this can be empty, but not nil.
}

type SOMHandler struct {
	mtx            sync.Mutex
	peer_container *pcn.PeerContainer //shared, read only

	//peer hash, world uuid, object
	//*SOMServeObjectGroup is created on first object share from host.
	serve_map *advancedmap.DuelKeyMap[string, string, *SOMServeObjectGroup]

	//object uuid
	objects map[string]*ws.SharedObject

	//event
	event_ch chan *SomEvent
}

func NewSOMHandler(peer_container *pcn.PeerContainer) *SOMHandler {
	return &SOMHandler{
		peer_container: peer_container,

		serve_map: advancedmap.NewDuelKeyMap[string, string, *SOMServeObjectGroup](),

		objects: make(map[string]*ws.SharedObject),

		event_ch: make(chan *SomEvent, 128),
	}
}

// unused event
func (m *SOMHandler) OnConnected(ctx context.Context, peer *pcn.Peer) error         { return nil }
func (m *SOMHandler) OnConnectFailed(ctx context.Context, address *aurl.AURL) error { return nil }
func (m *SOMHandler) OnClosed(ctx context.Context, peer *pcn.Peer) error            { return nil }

func (m *SOMHandler) ServeMessage(ctx context.Context, peer *pcn.Peer, frame *pcn.MessageFrame) error {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	switch frame.Type {
	case pcn.SOR:
		world_uuid, rem := functional.MaybeYield(functional.MakeMaybe(frame.Payload), serializer.LegacyDes(serializer.DeserializeString))
		if !rem.Ok() {
			return errors.New("SOR: failed to parse")
		}

		serve_obj_group, ok := m.serve_map.Get(peer.Address.Hash, world_uuid)
		if !ok {
			//no serve object group exist - respond with empty SO.
			return peer.SendSerializedMessageFrameSync(pcn.SO, serializer.SerializeJoinRaw([]*serializer.SerializedNode{
				serializer.SerializeString(world_uuid),
				serializer.SerializeEmptyArray(),
			}))
		}

		serve_obj_group.isActive = true
		if len(serve_obj_group.objects) == 0 {
			return peer.SendSerializedMessageFrameSync(pcn.SO, serializer.SerializeJoinRaw([]*serializer.SerializedNode{
				serializer.SerializeString(world_uuid),
				serializer.SerializeEmptyArray(),
			}))
		}

		err := peer.SendSerializedMessageFrameSync(pcn.SO, serializer.SerializeJoinRaw([]*serializer.SerializedNode{
			serializer.SerializeString(world_uuid),
			serializer.SerializeObjectArray(serve_obj_group.objects, ws.SerializeSharedObject),
		}))
		return err
	case pcn.SO:
		world_uuid, rem := functional.MaybeYield(functional.MakeMaybe(frame.Payload), serializer.LegacyDes(serializer.DeserializeString))
		objects, rem := functional.MaybeYield(rem, serializer.LegacyDesObjectArray(ws.DeserialzeSharedObject))
		if !rem.Ok() {
			return errors.New("SO: failed to parse")
		}

		m.event_ch <- &SomEvent{
			PeerHash:   peer.Address.Hash,
			WorldUUID:  world_uuid,
			Type:       SomReNew,
			SomObjects: objects,
		}
		return nil
	case pcn.SOA:
		world_uuid, rem := functional.MaybeYield(functional.MakeMaybe(frame.Payload), serializer.LegacyDes(serializer.DeserializeString))
		objects, rem := functional.MaybeYield(rem, serializer.LegacyDesObjectArray(ws.DeserialzeSharedObject))
		if !rem.Ok() {
			return errors.New("SOA: failed to parse")
		}

		m.event_ch <- &SomEvent{
			PeerHash:   peer.Address.Hash,
			WorldUUID:  world_uuid,
			Type:       SomAppend,
			SomObjects: objects,
		}
		return nil
	case pcn.SOD:
		world_uuid, rem := functional.MaybeYield(functional.MakeMaybe(frame.Payload), serializer.LegacyDes(serializer.DeserializeString))
		object_uuids, rem := functional.MaybeYield(rem, serializer.LegacyDes(serializer.DeserializeStringArray))
		if !rem.Ok() {
			return errors.New("SOD: failed to parse")
		}

		m.event_ch <- &SomEvent{
			PeerHash:    peer.Address.Hash,
			WorldUUID:   world_uuid,
			Type:        SomDelete,
			SomObjUUIDs: object_uuids,
		}
		return nil
	}
	return errors.ErrUnsupported
}

func (m *SOMHandler) RequestSOMService(peer_hash string, world_uuid string) error {
	peer, ok := m.peer_container.Get(peer_hash)
	if !ok {
		return errors.New("RequestSOMService: peer not connected")
	}

	m.mtx.Lock()
	defer m.mtx.Unlock()

	err := peer.SendSerializedMessageFrameSync(pcn.SOR, serializer.SerializeJoinRaw([]*serializer.SerializedNode{
		serializer.SerializeString(world_uuid),
		//TODO: additional fields
	}))

	return err
}

func (m *SOMHandler) InitiateSOMService(peer_hash string, world_uuid string) {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	m.serve_map.Set(peer_hash, world_uuid, &SOMServeObjectGroup{
		isActive: false,
		objects:  make([]*ws.SharedObject, 0),
	})
}

func (m *SOMHandler) TerminateSOMService(peer_hash string, world_uuid string) {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	m.serve_map.Delete(peer_hash, world_uuid)
}

func (m *SOMHandler) RegisterObject(object *ws.SharedObject) {
	m.mtx.Lock()
	defer m.mtx.Unlock()

	m.objects[object.UUID.String()] = object
}

func (m *SOMHandler) ShareObject(peer_hash string, world_uuid string, object_uuids []string) error {
	peer, ok := m.peer_container.Get(peer_hash)
	if !ok {
		return errors.New("RequestSOMService: peer not connected")
	}

	m.mtx.Lock()
	defer m.mtx.Unlock()

	registered_objects, ok := functional.Filter_strict_ok(object_uuids, func(uuid string) (*ws.SharedObject, bool) {
		obj, ok := m.objects[uuid]
		return obj, ok
	})
	if !ok {
		return errors.New("ShareObject: unregisterd objects")
	}

	serve_obj_group, ok := m.serve_map.Get(peer_hash, world_uuid)
	if !ok {
		return errors.New("ShareObject: uninitialized service group")
	}

	serve_obj_group.objects = append(serve_obj_group.objects, registered_objects...)
	if serve_obj_group.isActive {
		return peer.SendSerializedMessageFrameSync(pcn.SOA, serializer.SerializeJoinRaw([]*serializer.SerializedNode{
			serializer.SerializeString(world_uuid),
			serializer.SerializeObjectArray(registered_objects, ws.SerializeSharedObject),
		}))
	}
	//if not active, don't send SOA.
	return nil
}

func (m *SOMHandler) WaitEvent() *SomEvent {
	return <-m.event_ch
}
