package host

import "sync"

type TrustLevel int

const (
	Blocked TrustLevel = iota
	Connected
	Friend
	Trusted
)

type FireWallLevel int

const (
	Public FireWallLevel = iota
	FriendOnly
	Private
)

type FireWall struct {
	KnownHosts map[string]TrustLevel
	Level      int

	mutex sync.Mutex
}

func (f *FireWall) AppendHost(hash string, trust_level TrustLevel) {
	f.mutex.Lock()
	defer f.mutex.Unlock()

	f.KnownHosts[hash] = trust_level
}

func (f *FireWall) SetAllowLevel() {

}

func (f *FireWall) Check(hash string) bool {
	return false
}
