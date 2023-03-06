package guard

type NakamaRTMessage int

//go:generate stringer -type=NakamaRTMessage
const (
	ChannelJoin NakamaRTMessage = iota
	ChannelLeave
	ChannelMessageSend
	ChannelMessageUpdate
	ChannelMessageRemove
	MatchCreate
	MatchDataSend
	MatchJoin
	MatchLeave
	MatchmakerAdd
	MatchmakerRemove
	PartyCreate
	PartyJoin
	PartyLeave
	PartyPromote
	PartyAccept
	PartyRemove
	PartyClose
	PartyJoinRequestList
	PartyMatchmakerAdd
	PartyMatchmakerRemove
	PartyDataSend
	Ping
	Pong
	Rpc
	StatusFollow
	StatusUnfollow
	StatusUpdate
)

type NakamaMessage int

//go:generate stringer -type=NakamaMessage
const (
	GetAccount NakamaMessage = iota
	UpdateAccount
	SessionRefresh
	AuthenticateApple
	AuthenticateCustom
	AuthenticateDevice
	AuthenticateEmail
	AuthenticateFacebook
	AuthenticateFacebookInstantGame
	AuthenticateGameCenter
	AuthenticateGoogle
	AuthenticateSteam
	ListChannelMessages
	ListFriends
	AddFriends
	DeleteFriends
	BlockFriends
	ImportFacebookFriends
	CreateGroup
	UpdateGroup
	DeleteGroup
	JoinGroup
	LeaveGroup
	AddGroupUsers
	BanGroupUsers
	KickGroupUsers
	PromoteGroupUsers
	DemoteGroupUsers
	ListGroupUsers
	ListUserGroups
	ListGroups
	DeleteLeaderboardRecord
	ListLeaderboardRecords
	WriteLeaderboardRecord
	ListLeaderboardRecordsAroundOwner
	LinkApple
	LinkCustom
	LinkDevice
	LinkEmail
	LinkFacebook
	LinkFacebookInstantGame
	LinkGameCenter
	LinkGoogle
	LinkSteam
	ListMatches
	ListNotifications
	DeleteNotifications
	ListStorageObjects
	ReadStorageObjects
	WriteStorageObjects
	DeleteStorageObjects
	JoinTournament
	ListTournamentRecords
	ListTournaments
	WriteTournamentRecord
	ListTournamentRecordsAroundOwner
	UnlinkApple
	UnlinkCustom
	UnlinkDevice
	UnlinkEmail
	UnlinkFacebook
	UnlinkFacebookInstantGame
	UnlinkGameCenter
	UnlinkGoogle
	UnlinkSteam
	GetUsers
)
