package pcn

type FrameType uint64

// abyss neighbor discovery
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

// shared object model
const (
	SOR FrameType = iota + 20 //request
	SO                        //init
	SOA                       //new
	SOD                       //delete
)

// ping
const (
	PINGT FrameType = iota + 60
	PINGR
)
