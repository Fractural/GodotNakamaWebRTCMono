package rpc

import (
	"github.com/heroiclabs/nakama-common/runtime"
	"google.golang.org/grpc/codes"
)

var (
	ErrServer      = runtime.NewError("Server error", int(codes.Unavailable))
	ErrMarshalType = runtime.NewError("Cannot marshal type", int(codes.Unavailable))
)
