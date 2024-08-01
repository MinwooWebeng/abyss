package and

// type NeighborDiscoveryPeerState int

// const (
// 	NC_NI NeighborDiscoveryPeerState = iota + 10
// 	AC_NI
// 	AC_MM
// 	AC_PM
// 	AC_JN
// 	CC_JN
// 	CC_MR
// )

type INeighborDiscoveryPeerBase interface {
	SendJN(path string) error
	SendJOK(path string, world INeighborDiscoveryWorldBase, member_addrs []any) error
	SendJDN(path string, status int, message string) error
	SendJNI(world INeighborDiscoveryWorldBase, member INeighborDiscoveryPeerBase) error
	SendMEM(world INeighborDiscoveryWorldBase) error
	SendSNB(world INeighborDiscoveryWorldBase, members_hash []string) error
	SendCRR(world INeighborDiscoveryWorldBase, member_hash string) error
	SendRST(world_uuid string) error

	GetAddress() any
	GetHash() string
}
