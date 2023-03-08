package utils

import (
	"strings"

	. "github.com/ahmetb/go-linq/v3"
)

type stringable interface {
	String() string
}

func String[T stringable](slice []T) string {
	var results []string
	From(slice).SelectT(func(m T) string { return m.String() }).ToSlice(&results)
	return "[" + strings.Join(results, ", ") + "]"
}
