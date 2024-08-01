package pcn

type FrameType uint64

const (
	ID FrameType = iota
	JN
	JOK
	JDN
	JNI
	MEM
	SNB
	CRR
	RST
)

const (
	PINGT FrameType = iota + 60
	PINGR
)
