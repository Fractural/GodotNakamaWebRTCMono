using Godot;
using Nakama;
using System;
using System.Collections.Generic;
using GDC = Godot.Collections;

namespace NakamaWebRTC
{
    public enum MatchState
    {
        Lobby,
        Matching,
        Connecting,
        WaitingForEnoughPlayers,
        Ready,
        Playing
    }

    public enum MatchMode
    {
        None,
        Create,
        Join,
        Matchmaker,
    }

    public enum PlayerStatus
    {
        Connecting,
        Connected
    }

    public enum MatchOpCode
    {
        WebRTCPeerMethod = 9001,
        JoinSuccess = 9002,
        JoinError = 9003
    }

    public enum JoinErrorReason
    {
        MatchHasAlreadyBegun,
        MatchIsFull
    }

    public enum ErrorCode
    {
        MatchCreateFailed,
        JoinMatchFailed,
        StartMatchmakingFailed,
        WebsocketConnectionError,
        HostDisconnected,
        MatchmakerError,
        ClientVersionError,
        ClientJoinError,
        WebRTCOfferError,
    }

    public class IceServersConfig
    {
        public string[] Urls { get; set; } = new string[0];
    }

    public class OnlineMatch : Node
    {
        public int MinPlayers { get; set; } = 2;
        public int MaxPlayers { get; set; } = 4;
        public string ClientVersion { get; } = "dev";
        public IceServersConfig IceServersConfig { get; set; }
        public enum NetworkRelay
        {
            Auto,
            Forced,
            Disabled
        }
        public NetworkRelay UseNetworkRelay = NetworkRelay.Auto;

        // TODO Insert connection to nakama here
        public string MySessionID { get; private set; }
        public string MatchID { get; private set; }
        public string MatchmakerTicket { get; private set; }

        private WebRTCMultiplayer webrtcMultiplayer;
        private Dictionary<string, WebRTCPeerConnection> webrtcPeers;
        private Dictionary<string, bool> webrtcPeersConnected;

        public class Player : Godot.Object
        {
            public string SessionID { get; set; }
            public string Username { get; set; }
            public int PeerID { get; set; }

            public Player(string sessionID, string username, int peerID)
            {
                SessionID = sessionID;
                Username = username;
                PeerID = peerID;
            }

            public static Player FromPresence(IUserPresence presence, int peerID)
            {
                return new Player(presence.SessionId, presence.Username, peerID);
            }

            // TODO
            //public static Player FromDict(GDC.Dictionary)
        }

        public MatchState MatchState { get; set; }
        public static readonly IReadOnlyDictionary<JoinErrorReason, string> JoinErrorMessages = new Dictionary<JoinErrorReason, string>()
        {
            [JoinErrorReason.MatchHasAlreadyBegun] = "Sorry! The match has already begun.",
            [JoinErrorReason.MatchIsFull] = "Sorry! The match is full.",
        };
        public static readonly IReadOnlyDictionary<ErrorCode, string> ErrorMessages = new Dictionary<ErrorCode, string>()
        {
            [ErrorCode.MatchCreateFailed] = "Failed to create match",
            [ErrorCode.JoinMatchFailed] = "Unable to join match",
            [ErrorCode.StartMatchmakingFailed] = "Unable to join match making pool",
            [ErrorCode.WebsocketConnectionError] = "WebSocket connection error",
            [ErrorCode.HostDisconnected] = "Host has disconnectred",
            [ErrorCode.MatchmakerError] = "Matchmaker error",
            [ErrorCode.ClientVersionError] = "Client version doesn't match host",
            [ErrorCode.ClientJoinError] = "Client not allowed to join",
            [ErrorCode.WebRTCOfferError] = "Unable to create WebRTC offer",
        };

        [Signal]
        public delegate void Error(string message);
        [Signal]
        public delegate void ErrorCode(int code, string message, object extra); // TODO: Remove extra if unused. originally used for exceptions, which are not needed for C#
        // TODO: Rename this to avoid collision
        [Signal]
        public delegate void Disconnected();

        [Signal]
        public delegate void MatchCreated(string matchID);
        [Signal]
        public delegate void MatchJoined(string matchID);
        [Signal]
        public delegate void MatchmakerMatched(string matchID);

        [Signal]
        public delegate void PlayerJoined(Player player);
        [Signal]
        public delegate void PlayerLeft(Player player);
        [Signal]
        public delegate void PlayerStatusChanged(Player player, int status);

        public OnlineMatch()
        {
            IceServersConfig = new IceServersConfig();
            IceServersConfig.Urls = new[] { "stun:stun.l.google.com:19302" };
        }

        public override void _Ready()
        {
            var webrtcPeer = new WebRTCPeerConnection();
            webrtcPeer.Initialize();
        }
    }
}