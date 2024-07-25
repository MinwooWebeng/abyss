package ahmp

type MessageMux struct {
	IdHandleFunc  func(*AHMPRaw_ID) error
	JNHandleFunc  func(*AHMPRaw_JN) error
	JOKHandleFunc func(*AHMPRaw_JOK) error
	JDNHandleFunc func(*AHMPRaw_JDN) error
	JNIHandleFunc func(*AHMPRaw_JNI) error
	MEMHandleFunc func(*AHMPRaw_MEM) error
	SNBHandleFunc func(*AHMPRaw_SNB) error
	CRRHandleFunc func(*AHMPRaw_CRR) error
	RSTHandleFunc func(*AHMPRaw_RST) error
}

func (m *MessageMux) ServeMessage(frame *MessageFrame) error {
	switch frame.Type {
	case 0:
		return m.IdHandleFunc(&AHMPRaw_ID{cert: frame.Payload})
	case 1:
	case 2:
	case 3:
	case 4:
	case 5:
	case 6:
	case 7:
	case 8:
	case 9:
	}
}
