using NakamaWebRTC;
using System.Collections.Generic;

namespace NakamaWebRTCDemo
{
    public interface IOnlineGame
    {
        void MatchmakerMatched(IReadOnlyCollection<Player> players);
        void MatchCreated(string matchID);
        void MatchJoined(string matchID);
    }
}