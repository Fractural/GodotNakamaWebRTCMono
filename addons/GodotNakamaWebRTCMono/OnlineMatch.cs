using Godot;
using Nakama;
using System.Text.Json;
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

        private ISocket nakamaSocket;
        public ISocket NakamaSocket
        {
            get => nakamaSocket;
            private set
            {
                if (value == nakamaSocket)
                    return;

                if (nakamaSocket != null)
                {
                    nakamaSocket.Closed -= OnNakamaClosed;
                    nakamaSocket.ReceivedError -= OnNakamaError;
                    nakamaSocket.ReceivedMatchState -= OnNakamaMatchState;
                    nakamaSocket.ReceivedMatchPresence -= OnNakamaMatchPresence;
                    nakamaSocket.ReceivedMatchmakerMatched -= OnNakamaMatchmakerMatched;
                }

                nakamaSocket = value;

                if (nakamaSocket != null)
                {
                    nakamaSocket.Closed += OnNakamaClosed;
                    nakamaSocket.ReceivedError += OnNakamaError;
                    nakamaSocket.ReceivedMatchState += OnNakamaMatchState;
                    nakamaSocket.ReceivedMatchPresence += OnNakamaMatchPresence;
                    nakamaSocket.ReceivedMatchmakerMatched += OnNakamaMatchmakerMatched;
                }
            }
        }

        public string MySessionID { get; private set; }
        public string MatchID { get; private set; }
        public string MatchmakerTicket { get; private set; }

        private WebRTCMultiplayer webrtcMultiplayer;
        private Dictionary<string, WebRTCPeerConnection> webrtcPeers;
        private Dictionary<string, bool> webrtcPeersConnected;

        /// <summary>
        /// Session ID to Player dictionary
        /// </summary>
        public Dictionary<string, Player> Players { get; private set; }
        private int nextPeerID;

        public MatchMode MatchMode { get; private set; } = MatchMode.None;
        public MatchState MatchState { get; private set; } = MatchState.Lobby;
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

        // string message
        public event Action<string> OnError;
        // int code, string message, object extra
        public event Action<ErrorCode, string, object> OnErrorCode;
        // TODO: Remove extra if unused. originally used for exceptions, which are not needed for C#

        // TODO: Rename this to avoid collision
        public event Action Disconnected;

        // string matchID
        public event Action<string> MatchCreated;
        // string matchID
        public event Action<string> MatchJoined;
        // string matchID
        public event Action<string> MatchmakerMatched;

        // Player player
        public event Action<Player> PlayerJoined;
        // Player player
        public event Action<Player> PlayerLeft;
        // Player player, int status
        public event Action<Player, int> PlayerStatusChanged;

        // Player[] Players
        public event Action<Player[]> MatchReady;
        public event Action MatchNotReady;

        // WebRTCPeerConnection webrtcPeer, Player player
        public event Action<WebRTCPeerConnection, Player> WebRTCPeerAdded;
        // WebRTCPeerConnection webrtcPeer, Player player
        public event Action<WebRTCPeerConnection, Player> WebRTCPeerRemoved;

        [Serializable]
        public class Player
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
        }

        public class PlayersPayload
        {
            public Player[] Players { get; set; }
        }

        private string SerializePlayersPayload(PlayersPayload players)
        {
            return JsonSerializer.Serialize(players);
        }

        private PlayersPayload DeserializePlayersPayload(string json)
        {
            return JsonSerializer.Deserialize<PlayersPayload>(json);
        }

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


        private void EmitError(ErrorCode code, object extra)
        {
            string message = ErrorMessages[code];
            if (code == ErrorCode.ClientJoinError)
                message = JoinErrorMessages[(JoinErrorReason)extra];
            OnError?.Invoke(message);
            OnErrorCode?.Invoke(code, message, extra);
        }

        public async void CreateMatch(ISocket nakamaSocket)
        {
            Leave();
            NakamaSocket = nakamaSocket;

            try
            {
                IMatch match = await nakamaSocket.CreateMatchAsync();
                OnNakamaMatchCreated(match);
            }
            catch (ApiResponseException ex)
            {
                Leave();
                EmitError(ErrorCode.MatchCreateFailed, ex);
                // TODO: Maybe remove emit error altogether if error is already handled by the exception itself
            }
        }

        public async void JoinMatch(ISocket nakamaSocket, string matchID)
        {
            Leave();
            NakamaSocket = nakamaSocket;
            MatchMode = MatchMode.Join;

            try
            {
                IMatch match = await NakamaSocket.JoinMatchAsync(matchID);
                OnNakamaMatchJoined(match);
            }
            catch (ApiResponseException ex)
            {
                Leave();
                EmitError(ErrorCode.JoinMatchFailed, ex);
            }
        }

        public class MatchmakingArgs
        {
            public string Query = "*";
            public int MinCount = 2;
            public int MaxCount = 8;
            public Dictionary<string, string> stringProperties = null;
            public Dictionary<string, double> numericProperties = null;
            public int? countMultiple = null;
        }

        public void StartMatchmaking(ISocket nakamaSocket, MatchmakingArgs args)
        {
            Leave();
            NakamaSocket = nakamaSocket;
            MatchMode = MatchMode.Matchmaker;

            nakamaSocket.AddMatchmakerAsync(args.Query, args.MinCount, args.MaxCount, args.stringProperties, args.numericProperties, args.countMultiple);
        }

        private void OnNakamaMatchJoined(IMatch match)
        {
            throw new NotImplementedException();
        }

        private void OnNakamaMatchCreated(IMatch match)
        {
            throw new NotImplementedException();
        }

        private void Leave()
        {
            throw new NotImplementedException();
        }

        private void OnNakamaMatchmakerMatched(IMatchmakerMatched obj)
        {
            throw new NotImplementedException();
        }

        private void OnNakamaMatchPresence(IMatchPresenceEvent obj)
        {
            throw new NotImplementedException();
        }

        private void OnNakamaMatchState(IMatchState obj)
        {
            throw new NotImplementedException();
        }

        private void OnNakamaError(Exception obj)
        {
            throw new NotImplementedException();
        }

        private void OnNakamaClosed()
        {
            throw new NotImplementedException();
        }
    }
}