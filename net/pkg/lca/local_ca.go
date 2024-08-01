package lca

import (
	"crypto/ed25519"
	"crypto/rand"
	"crypto/x509"
	"crypto/x509/pkix"
	"errors"
	"math/big"
	mrand "math/rand/v2"
	"reflect"
	"time"

	"github.com/mr-tron/base58/base58"
	"golang.org/x/crypto/sha3"
)

type LocalCA struct {
	Name           string
	IdentityPubKey any
	IdentityCert   []byte

	LocalCert    *x509.Certificate
	LocalPrivKey ed25519.PrivateKey
	LocalPubKey  ed25519.PublicKey
}

func HashPubkey(identity_pubkey any) (string, error) {
	switch v := identity_pubkey.(type) {
	case ed25519.PublicKey:
		hashfunc := sha3.New256()
		hashfunc.Write(v)
		hash := hashfunc.Sum(nil)
		return base58.Encode(hash), nil
	default:
		return "", errors.New("unknown public key type: " + reflect.TypeOf(v).Name())
	}
}

func NewLocalCA(name string, identity_pubkey any) (*LocalCA, error) {
	ed25519_public_key, ed25519_private_key, err := ed25519.GenerateKey(rand.Reader)
	if err != nil {
		return nil, err
	}

	return &LocalCA{
		LocalCert: &x509.Certificate{
			SerialNumber: big.NewInt(mrand.Int64()),
			Subject: pkix.Name{
				CommonName:   name,
				SerialNumber: "SHA3-256:",
			},
			NotBefore:   time.Now(),
			NotAfter:    time.Now().AddDate(0, 1, 0),
			IsCA:        true,
			ExtKeyUsage: []x509.ExtKeyUsage{x509.ExtKeyUsageClientAuth, x509.ExtKeyUsageCodeSigning, x509.ExtKeyUsageServerAuth},
			KeyUsage:    x509.KeyUsageDigitalSignature | x509.KeyUsageCertSign,
		},
		LocalPrivKey: ed25519_private_key,
		LocalPubKey:  ed25519_public_key,
	}, nil
}
