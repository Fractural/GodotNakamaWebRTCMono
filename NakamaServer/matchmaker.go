package main

import (
	"context"
	"database/sql"
	"encoding/json"

	"github.com/heroiclabs/nakama-common/runtime"
)

type MatchMakeResponse struct {
	Success bool `json:"success"`
}

func RpcMatchMake(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, payload string) (string, error) {
	logger.Debug("Healthcheck RPC called")
	response := &HealthCheckResponse{Success: true}

	out, err := json.Marshal(response)
	if err != nil {
		logger.Error("Error marshalling response type to JSON: %v", err)
		return "", runtime.NewError("Cannot marshal type", 13)
	}

	return string(out), nil
}
