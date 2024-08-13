package advancedmap

type DuelKeyMap[K1 comparable, K2 comparable, V any] struct {
	map_L map[K1]map[K2]V
	map_R map[K2]map[K1]V
}

func NewDuelKeyMap[K1 comparable, K2 comparable, V any]() *DuelKeyMap[K1, K2, V] {
	return &DuelKeyMap[K1, K2, V]{
		map_L: make(map[K1]map[K2]V),
		map_R: make(map[K2]map[K1]V),
	}
}

func (m *DuelKeyMap[K1, K2, V]) Set(Lkey K1, Rkey K2, value V) {
	L_e, ok := m.map_L[Lkey]
	if !ok {
		L_e = make(map[K2]V)
		m.map_L[Lkey] = L_e
	}
	R_e, ok := m.map_R[Rkey]
	if !ok {
		R_e = make(map[K1]V)
		m.map_R[Rkey] = R_e
	}

	L_e[Rkey] = value
	R_e[Lkey] = value
}

func (m *DuelKeyMap[K1, K2, V]) Get(Lkey K1, Rkey K2) (V, bool) {
	var result V

	R_e, ok := m.map_L[Lkey]
	if !ok {
		return result, false
	}

	result, ok = R_e[Rkey]
	if !ok {
		return result, false
	}

	return result, true
}

func (m *DuelKeyMap[K1, K2, V]) ForeachL(key K1, f func(V, K2)) {
	R_e, ok := m.map_L[key]
	if !ok {
		return
	}

	for r_k, v := range R_e {
		f(v, r_k)
	}
}

func (m *DuelKeyMap[K1, K2, V]) ForeachR(key K2, f func(V, K1)) {
	L_e, ok := m.map_R[key]
	if !ok {
		return
	}

	for l_e, v := range L_e {
		f(v, l_e)
	}
}

func (m *DuelKeyMap[K1, K2, V]) DeleteL(key K1) {
	R_V_map, ok := m.map_L[key]
	if !ok {
		return
	}

	for k_R, _ := range R_V_map {
		L_V_map, ok := m.map_R[k_R]
		if !ok {
			panic("DuelKeyMap: corrupted")
		}

		delete(L_V_map, key)
		if len(L_V_map) == 0 {
			delete(m.map_R, k_R)
		}
	}

	delete(m.map_L, key)
}

func (m *DuelKeyMap[K1, K2, V]) DeleteR(key K2) {
	L_V_map, ok := m.map_R[key]
	if !ok {
		return
	}

	for k_L, _ := range L_V_map {
		R_V_map, ok := m.map_L[k_L]
		if !ok {
			panic("DuelKeyMap: corrupted")
		}

		delete(R_V_map, key)
		if len(R_V_map) == 0 {
			delete(m.map_L, k_L)
		}
	}

	delete(m.map_R, key)
}

func (m *DuelKeyMap[K1, K2, V]) Delete(Lkey K1, Rkey K2) {
	R_V_map, ok_L := m.map_L[Lkey]
	L_V_map, ok_R := m.map_R[Rkey]

	if ok_L != ok_R {
		panic("DuelKeyMap: corrupted")
	}
	if !ok_L {
		return
	}

	delete(R_V_map, Rkey)
	if len(R_V_map) == 0 {
		delete(m.map_L, Lkey)
	}
	delete(L_V_map, Lkey)
	if len(L_V_map) == 0 {
		delete(m.map_R, Rkey)
	}
}
