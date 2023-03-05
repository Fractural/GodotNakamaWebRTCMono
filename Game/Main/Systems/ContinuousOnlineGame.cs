using Godot;
using NakamaWebRTC;
using System;
using System.Collections.Generic;

namespace NakamaWebRTCDemo
{
    /// <summary>
    /// Wraps around Game to handle Nakama online stuff.
    /// Supports joining mid match.
    /// </summary>
    public partial class ContinuousOnlineGame : Node, IOnlineGame
    {
        public void MatchCreated(string matchID)
        {
            throw new NotImplementedException();
        }

        public void MatchJoined(string matchID)
        {
            throw new NotImplementedException();
        }

        public void MatchmakerMatched(IReadOnlyCollection<Player> players)
        {
            throw new NotImplementedException();
        }
    }
}