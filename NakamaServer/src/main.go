package main

import (
	"context"
	"database/sql"
	"time"

	"github.com/fractural/godotnakamawebrtcmono/nakamaserver/guard"
	"github.com/fractural/godotnakamawebrtcmono/nakamaserver/rpc"
	"github.com/heroiclabs/nakama-common/runtime"
)

func InitModule(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, initializer runtime.Initializer) error {
	initStart := time.Now()

	if err := rpc.RegisterRPCs(initializer); err != nil {
		return err
	}

	if err := guard.RegisterGuards(initializer); err != nil {
		return err
	}

	logger.Info("Module loaded in %dms", time.Since(initStart).Milliseconds())
	return nil
}
