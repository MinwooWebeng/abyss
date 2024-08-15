package generr

import (
	"abyss/net/pkg/functional"
	"slices"
	"strings"
)

type MultiError[E error] struct {
	Errors []E
}

func NewSingleMultiError[E error](err E) *MultiError[E] {
	return &MultiError[E]{Errors: []E{err}}
}

func JoinMultiErrors[E error](input []*MultiError[E]) *MultiError[E] {
	return &MultiError[E]{
		Errors: slices.Concat(functional.Filter(input, func(e *MultiError[E]) []E { return e.Errors })...),
	}
}

func (e *MultiError[E]) Error() string {
	return "[" + strings.Join(functional.Filter(e.Errors, func(e E) string { return e.Error() }), ", ") + "]"
}
