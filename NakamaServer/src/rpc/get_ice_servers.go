package rpc

import (
	"context"
	"database/sql"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"net/http"
	"strings"

	"github.com/heroiclabs/nakama-common/runtime"
)

type GetIceServersResponse struct {
	Response json.RawMessage `json:"response"`
}

/*
	RpcGetIceServers expected the following information from runtime environment variables:

    runtime:
      env:
        - "twilio_account_sid=your_account_sid"
        - "twilio_auth_token=your_auth_token"
        - "twilio_turn_credentials_ttl=number seconds that TURN credentials are valid"
*/
func RpcGetIceServers(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, payload string) (string, error) {
	env := ctx.Value(runtime.RUNTIME_CTX_ENV).(map[string]string)

	// Create twilio http request
	twilioAccountSID := env["twilio_account_sid"]
	twilioAuthToken := env["twilio_auth_token"]
	url := fmt.Sprintf("https://api.twilio.com/2010-04-01/Accounts/%s/Tokens.json", twilioAccountSID)
	body := fmt.Sprintf("Ttl=%s", env["twilio_turn_credentials_ttl"])

	twilioReq, err := http.NewRequest("POST", url, strings.NewReader(body))
	if err != nil {
		logger.Error("Error creating POST for twilio: %v", err)
		return "", ErrServer
	}

	// Add headers
	credentials := base64.StdEncoding.EncodeToString([]byte(fmt.Sprintf("%s:%s", twilioAccountSID, twilioAuthToken)))
	twilioReq.Header.Set("Authorization", fmt.Sprintf("Basic %s", credentials))
	twilioReq.Header.Set("Content-Type", "application/x-www-form-urlencoded")

	// Send request
	twilioRes, err := http.DefaultClient.Do(twilioReq)
	if err != nil {
		logger.Error("Error sending POST to twilio: %v", err)
		return "", ErrServer
	}

	// Get request body as string
	twilioResBody, err := ioutil.ReadAll(twilioRes.Body)
	if err != nil {
		logger.Error("Error reading twilio request body: %v", err)
		return "", ErrServer
	}

	// Make sure status code is StatusCreated (success)
	if twilioRes.StatusCode != http.StatusCreated {
		logger.Error("Fetching twilio ice servers failed")
		return "", ErrServer
	}

	// Return result if
	response, err := json.Marshal(&GetIceServersResponse{
		Response: json.RawMessage(twilioResBody),
	})
	if err != nil {
		logger.Error("Error marshalling response type to JSON: %v", err)
		return "", ErrMarshalType
	}

	return string(response), nil
}
