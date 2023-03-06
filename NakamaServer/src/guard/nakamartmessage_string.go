// Code generated by "stringer -type=NakamaRTMessage"; DO NOT EDIT.

package guard

import "strconv"

func _() {
	// An "invalid array index" compiler error signifies that the constant values have changed.
	// Re-run the stringer command to generate them again.
	var x [1]struct{}
	_ = x[ChannelJoin-0]
	_ = x[ChannelLeave-1]
	_ = x[ChannelMessageSend-2]
	_ = x[ChannelMessageUpdate-3]
	_ = x[ChannelMessageRemove-4]
	_ = x[MatchCreate-5]
	_ = x[MatchDataSend-6]
	_ = x[MatchJoin-7]
	_ = x[MatchLeave-8]
	_ = x[MatchmakerAdd-9]
	_ = x[MatchmakerRemove-10]
	_ = x[PartyCreate-11]
	_ = x[PartyJoin-12]
	_ = x[PartyLeave-13]
	_ = x[PartyPromote-14]
	_ = x[PartyAccept-15]
	_ = x[PartyRemove-16]
	_ = x[PartyClose-17]
	_ = x[PartyJoinRequestList-18]
	_ = x[PartyMatchmakerAdd-19]
	_ = x[PartyMatchmakerRemove-20]
	_ = x[PartyDataSend-21]
	_ = x[Ping-22]
	_ = x[Pong-23]
	_ = x[Rpc-24]
	_ = x[StatusFollow-25]
	_ = x[StatusUnfollow-26]
	_ = x[StatusUpdate-27]
}

const _NakamaRTMessage_name = "ChannelJoinChannelLeaveChannelMessageSendChannelMessageUpdateChannelMessageRemoveMatchCreateMatchDataSendMatchJoinMatchLeaveMatchmakerAddMatchmakerRemovePartyCreatePartyJoinPartyLeavePartyPromotePartyAcceptPartyRemovePartyClosePartyJoinRequestListPartyMatchmakerAddPartyMatchmakerRemovePartyDataSendPingPongRpcStatusFollowStatusUnfollowStatusUpdate"

var _NakamaRTMessage_index = [...]uint16{0, 11, 23, 41, 61, 81, 92, 105, 114, 124, 137, 153, 164, 173, 183, 195, 206, 217, 227, 247, 265, 286, 299, 303, 307, 310, 322, 336, 348}

func (i NakamaRTMessage) String() string {
	if i < 0 || i >= NakamaRTMessage(len(_NakamaRTMessage_index)-1) {
		return "NakamaRTMessage(" + strconv.FormatInt(int64(i), 10) + ")"
	}
	return _NakamaRTMessage_name[_NakamaRTMessage_index[i]:_NakamaRTMessage_index[i+1]]
}
