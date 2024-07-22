package conn

import "abyss/net/pkg/aurl"

type ConnectionManager struct {
}

func NewConnectionManager() *ConnectionManager {
	result := new(ConnectionManager)
	return result
}

func (m *ConnectionManager) Dial(url *aurl.AbyssURL) {

}

//func (m *ConnectionManager)
