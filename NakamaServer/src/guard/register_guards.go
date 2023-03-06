package guard

import (
	"context"
	"database/sql"

	"github.com/heroiclabs/nakama-common/api"
	"github.com/heroiclabs/nakama-common/rtapi"
	"github.com/heroiclabs/nakama-common/runtime"
)

var enabledRtMessages = []NakamaRTMessage{
	MatchJoin,
	MatchCreate,
	MatchLeave,
}

var enabledMessages = []NakamaMessage{
	AuthenticateEmail,
}

func enabledMessagesContains(message NakamaMessage) bool {
	for _, enabledMessage := range enabledMessages {
		if enabledMessage == message {
			return true
		}
	}
	return false
}

func removeAt[T any](slice []T, i int) []T {
	slice[i] = slice[len(slice)-1]
	return slice[:len(slice)-1]
}

func remove[T comparable](slice []T, value T) []T {
	for i, currValue := range slice {
		if currValue == value {
			return removeAt(slice, i)
		}
	}
	return slice
}

/*
RegisterGuards is a function that disables the API for messages that are not in use.
The enabled APIs are stored in the enabledRTMessages and enabledMessages arrays.
*/
func RegisterGuards(initializer runtime.Initializer) error {
	// Create disabled arrays by including only the messages
	// that are not present in the enabled arrays.
	disabledRtMessages := make([]NakamaRTMessage, len(_NakamaRTMessage_index))
	for i := 0; i < len(disabledRtMessages); i++ {
		disabledRtMessages[i] = NakamaRTMessage(i)
	}
	for i := 0; i < len(enabledRtMessages); i++ {
		disabledRtMessages = remove(disabledRtMessages, enabledRtMessages[i])
	}

	disabledMessages := make([]NakamaMessage, len(_NakamaRTMessage_index))
	for i := 0; i < len(disabledMessages); i++ {
		disabledMessages[i] = NakamaMessage(i)
	}
	for i := 0; i < len(enabledMessages); i++ {
		disabledMessages = remove(disabledMessages, enabledMessages[i])
	}

	for _, rtMessage := range disabledRtMessages {
		if err := initializer.RegisterBeforeRt(rtMessage.String(), func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, envelope *rtapi.Envelope) (*rtapi.Envelope, error) {
			return nil, nil
		}); err != nil {
			return err
		}
	}

	for _, message := range disabledMessages {
		switch message {
		case GetAccount:
			if err := initializer.RegisterBeforeGetAccount(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule) error {
				return nil
			}); err != nil {
				return err
			}
		case UpdateAccount:
			if err := initializer.RegisterBeforeUpdateAccount(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.UpdateAccountRequest) (*api.UpdateAccountRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case SessionRefresh:
			if err := initializer.RegisterBeforeSessionRefresh(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.SessionRefreshRequest) (*api.SessionRefreshRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case AuthenticateApple:
			if err := initializer.RegisterBeforeAuthenticateApple(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AuthenticateAppleRequest) (*api.AuthenticateAppleRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case AuthenticateCustom:
			if err := initializer.RegisterBeforeAuthenticateCustom(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AuthenticateCustomRequest) (*api.AuthenticateCustomRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case AuthenticateDevice:
			if err := initializer.RegisterBeforeAuthenticateDevice(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AuthenticateDeviceRequest) (*api.AuthenticateDeviceRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case AuthenticateEmail:
			if err := initializer.RegisterBeforeAuthenticateEmail(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AuthenticateEmailRequest) (*api.AuthenticateEmailRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case AuthenticateFacebook:
			if err := initializer.RegisterBeforeAuthenticateFacebook(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AuthenticateFacebookRequest) (*api.AuthenticateFacebookRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case AuthenticateFacebookInstantGame:
			if err := initializer.RegisterBeforeAuthenticateFacebookInstantGame(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AuthenticateFacebookInstantGameRequest) (*api.AuthenticateFacebookInstantGameRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case AuthenticateGameCenter:
			if err := initializer.RegisterBeforeAuthenticateGameCenter(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AuthenticateGameCenterRequest) (*api.AuthenticateGameCenterRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case AuthenticateGoogle:
			if err := initializer.RegisterBeforeAuthenticateGoogle(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AuthenticateGoogleRequest) (*api.AuthenticateGoogleRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case AuthenticateSteam:
			if err := initializer.RegisterBeforeAuthenticateSteam(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AuthenticateSteamRequest) (*api.AuthenticateSteamRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListChannelMessages:
			if err := initializer.RegisterBeforeListChannelMessages(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListChannelMessagesRequest) (*api.ListChannelMessagesRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListFriends:
			if err := initializer.RegisterBeforeListFriends(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListFriendsRequest) (*api.ListFriendsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case AddFriends:
			if err := initializer.RegisterBeforeAddFriends(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AddFriendsRequest) (*api.AddFriendsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case DeleteFriends:
			if err := initializer.RegisterBeforeDeleteFriends(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.DeleteFriendsRequest) (*api.DeleteFriendsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case BlockFriends:
			if err := initializer.RegisterBeforeBlockFriends(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.BlockFriendsRequest) (*api.BlockFriendsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ImportFacebookFriends:
			if err := initializer.RegisterBeforeImportFacebookFriends(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ImportFacebookFriendsRequest) (*api.ImportFacebookFriendsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case CreateGroup:
			if err := initializer.RegisterBeforeCreateGroup(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.CreateGroupRequest) (*api.CreateGroupRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case UpdateGroup:
			if err := initializer.RegisterBeforeUpdateGroup(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.UpdateGroupRequest) (*api.UpdateGroupRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case DeleteGroup:
			if err := initializer.RegisterBeforeDeleteGroup(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.DeleteGroupRequest) (*api.DeleteGroupRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case JoinGroup:
			if err := initializer.RegisterBeforeJoinGroup(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.JoinGroupRequest) (*api.JoinGroupRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case LeaveGroup:
			if err := initializer.RegisterBeforeLeaveGroup(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.LeaveGroupRequest) (*api.LeaveGroupRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case AddGroupUsers:
			if err := initializer.RegisterBeforeAddGroupUsers(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AddGroupUsersRequest) (*api.AddGroupUsersRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case BanGroupUsers:
			if err := initializer.RegisterBeforeBanGroupUsers(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.BanGroupUsersRequest) (*api.BanGroupUsersRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case KickGroupUsers:
			if err := initializer.RegisterBeforeKickGroupUsers(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.KickGroupUsersRequest) (*api.KickGroupUsersRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case PromoteGroupUsers:
			if err := initializer.RegisterBeforePromoteGroupUsers(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.PromoteGroupUsersRequest) (*api.PromoteGroupUsersRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case DemoteGroupUsers:
			if err := initializer.RegisterBeforeDemoteGroupUsers(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.DemoteGroupUsersRequest) (*api.DemoteGroupUsersRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListGroupUsers:
			if err := initializer.RegisterBeforeListGroupUsers(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListGroupUsersRequest) (*api.ListGroupUsersRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListUserGroups:
			if err := initializer.RegisterBeforeListUserGroups(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListUserGroupsRequest) (*api.ListUserGroupsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListGroups:
			if err := initializer.RegisterBeforeListGroups(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListGroupsRequest) (*api.ListGroupsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case DeleteLeaderboardRecord:
			if err := initializer.RegisterBeforeDeleteLeaderboardRecord(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.DeleteLeaderboardRecordRequest) (*api.DeleteLeaderboardRecordRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListLeaderboardRecords:
			if err := initializer.RegisterBeforeListLeaderboardRecords(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListLeaderboardRecordsRequest) (*api.ListLeaderboardRecordsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case WriteLeaderboardRecord:
			if err := initializer.RegisterBeforeWriteLeaderboardRecord(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.WriteLeaderboardRecordRequest) (*api.WriteLeaderboardRecordRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListLeaderboardRecordsAroundOwner:
			if err := initializer.RegisterBeforeListLeaderboardRecordsAroundOwner(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListLeaderboardRecordsAroundOwnerRequest) (*api.ListLeaderboardRecordsAroundOwnerRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case LinkApple:
			if err := initializer.RegisterBeforeLinkApple(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountApple) (*api.AccountApple, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case LinkCustom:
			if err := initializer.RegisterBeforeLinkCustom(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountCustom) (*api.AccountCustom, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case LinkDevice:
			if err := initializer.RegisterBeforeLinkDevice(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountDevice) (*api.AccountDevice, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case LinkEmail:
			if err := initializer.RegisterBeforeLinkEmail(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountEmail) (*api.AccountEmail, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case LinkFacebook:
			if err := initializer.RegisterBeforeLinkFacebook(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.LinkFacebookRequest) (*api.LinkFacebookRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case LinkFacebookInstantGame:
			if err := initializer.RegisterBeforeLinkFacebookInstantGame(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountFacebookInstantGame) (*api.AccountFacebookInstantGame, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case LinkGameCenter:
			if err := initializer.RegisterBeforeLinkGameCenter(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountGameCenter) (*api.AccountGameCenter, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case LinkGoogle:
			if err := initializer.RegisterBeforeLinkGoogle(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountGoogle) (*api.AccountGoogle, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case LinkSteam:
			if err := initializer.RegisterBeforeLinkSteam(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.LinkSteamRequest) (*api.LinkSteamRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListMatches:
			if err := initializer.RegisterBeforeListMatches(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListMatchesRequest) (*api.ListMatchesRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListNotifications:
			if err := initializer.RegisterBeforeListNotifications(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListNotificationsRequest) (*api.ListNotificationsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case DeleteNotifications:
			if err := initializer.RegisterBeforeDeleteNotifications(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.DeleteNotificationsRequest) (*api.DeleteNotificationsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListStorageObjects:
			if err := initializer.RegisterBeforeListStorageObjects(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListStorageObjectsRequest) (*api.ListStorageObjectsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ReadStorageObjects:
			if err := initializer.RegisterBeforeReadStorageObjects(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ReadStorageObjectsRequest) (*api.ReadStorageObjectsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case WriteStorageObjects:
			if err := initializer.RegisterBeforeWriteStorageObjects(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.WriteStorageObjectsRequest) (*api.WriteStorageObjectsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case DeleteStorageObjects:
			if err := initializer.RegisterBeforeDeleteStorageObjects(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.DeleteStorageObjectsRequest) (*api.DeleteStorageObjectsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case JoinTournament:
			if err := initializer.RegisterBeforeJoinTournament(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.JoinTournamentRequest) (*api.JoinTournamentRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListTournamentRecords:
			if err := initializer.RegisterBeforeListTournamentRecords(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListTournamentRecordsRequest) (*api.ListTournamentRecordsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListTournaments:
			if err := initializer.RegisterBeforeListTournaments(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListTournamentsRequest) (*api.ListTournamentsRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case WriteTournamentRecord:
			if err := initializer.RegisterBeforeWriteTournamentRecord(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.WriteTournamentRecordRequest) (*api.WriteTournamentRecordRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case ListTournamentRecordsAroundOwner:
			if err := initializer.RegisterBeforeListTournamentRecordsAroundOwner(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.ListTournamentRecordsAroundOwnerRequest) (*api.ListTournamentRecordsAroundOwnerRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case UnlinkApple:
			if err := initializer.RegisterBeforeUnlinkApple(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountApple) (*api.AccountApple, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case UnlinkCustom:
			if err := initializer.RegisterBeforeUnlinkCustom(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountCustom) (*api.AccountCustom, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case UnlinkDevice:
			if err := initializer.RegisterBeforeUnlinkDevice(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountDevice) (*api.AccountDevice, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case UnlinkEmail:
			if err := initializer.RegisterBeforeUnlinkEmail(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountEmail) (*api.AccountEmail, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case UnlinkFacebook:
			if err := initializer.RegisterBeforeUnlinkFacebook(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountFacebook) (*api.AccountFacebook, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case UnlinkFacebookInstantGame:
			if err := initializer.RegisterBeforeUnlinkFacebookInstantGame(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountFacebookInstantGame) (*api.AccountFacebookInstantGame, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case UnlinkGameCenter:
			if err := initializer.RegisterBeforeUnlinkGameCenter(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountGameCenter) (*api.AccountGameCenter, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case UnlinkGoogle:
			if err := initializer.RegisterBeforeUnlinkGoogle(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountGoogle) (*api.AccountGoogle, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case UnlinkSteam:
			if err := initializer.RegisterBeforeUnlinkSteam(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.AccountSteam) (*api.AccountSteam, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		case GetUsers:
			if err := initializer.RegisterBeforeGetUsers(func(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, in *api.GetUsersRequest) (*api.GetUsersRequest, error) {
				return nil, nil
			}); err != nil {
				return err
			}
		}
	}

	return nil
}
