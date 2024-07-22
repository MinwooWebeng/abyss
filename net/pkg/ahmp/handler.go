package ahmp

type Handler interface {
	ServeAHMP(ResponseWriter, *Request)
}
