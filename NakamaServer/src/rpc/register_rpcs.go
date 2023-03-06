package rpc

import (
	"github.com/heroiclabs/nakama-common/runtime"
)

func RegisterRPCs(initializer runtime.Initializer) error {
	if err := initializer.RegisterRpc("health_check", RpcHealthCheck); err != nil {
		return err
	}

	if err := initializer.RegisterRpc("get_ice_servers", RpcGetIceServers); err != nil {
		return err
	}
	return nil
}
