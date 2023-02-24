using Godot;
using Nakama;
using System.Text.Json;
using System;
using System.Collections.Generic;
using GDC = Godot.Collections;
using System.Diagnostics;
using System.Text;

namespace NakamaWebRTC
{
    // NOTE: PeerID is from WebRTC
    //       SessionID is from Nakama
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
        public string ClientVersion => "dev";
        public IceServersConfig IceServersConfig { get; set; } = new IceServersConfig()
        {
            Urls = new[] { "stun:stun.l.google.com:19302" },
        };
        public MatchmakingArgs DefaultMatchmakingArgs { get; private set; } = new MatchmakingArgs()
        {
            MinCount = 2,
            MaxCount = 4
        };
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
        public IMatchmakerTicket MatchmakerTicket { get; private set; }

        private WebRTCMultiplayer webrtcMultiplayer;
        private Dictionary<string, WebRTCPeerConnection> webrtcPeers;
        private Dictionary<string, bool> webrtcPeersConnected;

        public IEnumerable<string> SessionIDs => sessionIDToPlayers.Keys;
        public IEnumerable<Player> Players => sessionIDToPlayers.Values;
        /// <summary>
        /// Session ID to Player dictionary
        /// </summary>
        private Dictionary<string, Player> sessionIDToPlayers;
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
        // IEnumerable<Player> players
        public event Action<IEnumerable<Player>> MatchmakerMatched;

        // Player player
        public event Action<Player> PlayerJoined;
        // Player player
        public event Action<Player> PlayerLeft;
        // Player player, PlayerStatus status
        public event Action<Player, PlayerStatus> PlayerStatusChanged;

        // IEnumerable<Player> Players
        public event Action<IEnumerable<Player>> MatchReady;
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

        #region Match State Payloads
        public class JoinSuccessPayload
        {
            /// <summary>
            /// Session ID to player
            /// </summary>
            public Dictionary<string, Player> Players { get; set; }
            public string ClientVersion { get; set; }
        }

        public class JoinErrorPayload
        {
            /// <summary>
            /// Target player's session ID
            /// </summary>
            public string Target { get; set; }
            public JoinErrorReason Code { get; set; }
            public string Reason { get; set; }
        }
        #endregion

        // TODO: Remove if unneeeded
        //private string SerializePlayersPayload(PlayersPayload players)
        //{
        //    return JsonSerializer.Serialize(players);
        //}

        //private PlayersPayload DeserializePlayersPayload(string json)
        //{
        //    return JsonSerializer.Deserialize<PlayersPayload>(json);
        //}

        public override void _Ready()
        {
            var webrtcPeer = new WebRTCPeerConnection();
            webrtcPeer.Initialize();
        }


        private void EmitError(ErrorCode code, object extra = null)
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

        public async void StartMatchmaking(ISocket nakamaSocket, MatchmakingArgs args = null)
        {
            Leave();
            NakamaSocket = nakamaSocket;
            MatchMode = MatchMode.Matchmaker;
            if (args == null)
                args = DefaultMatchmakingArgs;

            if (ClientVersion != "")
            {
                if (args.stringProperties == null)
                    args.stringProperties = new Dictionary<string, string>();
                args.stringProperties["client_version"] = ClientVersion;

                string query = "+properties.client_version:" + ClientVersion;
                if (args.Query == "*")
                    args.Query = query;
                else
                    args.Query += " " + query;
            }

            MatchState = MatchState.Matching;

            try
            {
                MatchmakerTicket = await nakamaSocket.AddMatchmakerAsync(args.Query, args.MinCount, args.MaxCount, args.stringProperties, args.numericProperties, args.countMultiple);
            }
            catch (ApiResponseException ex)
            {
                Leave();
                EmitError(ErrorCode.StartMatchmakingFailed, ex);
            }
        }

        public void StartPlaying()
        {
            Debug.Assert(MatchState == MatchState.Ready);
            MatchState = MatchState.Playing;
        }

        public async void Leave(bool closeSocket = false)
        {
            if (webrtcMultiplayer != null)
            {
                webrtcMultiplayer.Close();
                GetTree().NetworkPeer = null;
            }

            if (NakamaSocket != null)
            {
                if (MatchID != "")
                    await NakamaSocket.LeaveMatchAsync(MatchID);
                else if (MatchmakerTicket != null)
                    await NakamaSocket.RemoveMatchmakerAsync(MatchmakerTicket);
                if (closeSocket)
                {
                    await nakamaSocket.CloseAsync();
                    nakamaSocket = null;
                }
            }

            MySessionID = "";
            MatchID = "";
            MatchmakerTicket = null;
            CreateWebRTCMultiplayer();
            webrtcPeers = new Dictionary<string, WebRTCPeerConnection>();
            webrtcPeersConnected = new Dictionary<string, bool>();
            sessionIDToPlayers = new Dictionary<string, Player>();
            nextPeerID = 1;
            MatchState = MatchState.Lobby;
            MatchMode = MatchMode.None;
        }

        private void CreateWebRTCMultiplayer()
        {
            if (webrtcMultiplayer != null)
            {
                webrtcMultiplayer.Disconnect("peer_connected", this, nameof(OnWebRTCPeerConnected));
                webrtcMultiplayer.Disconnect("peer_disconnected", this, nameof(OnWebRTCPeerDisconnected));
            }

            webrtcMultiplayer = new WebRTCMultiplayer();
            webrtcMultiplayer.Connect("peer_connected", this, nameof(OnWebRTCPeerConnected));
            webrtcMultiplayer.Connect("peer_disconnected", this, nameof(OnWebRTCPeerDisconnected));
        }

        public bool HasSessionID(int peerID) => GetPlayerByPeerID(peerID) != null;

        public Player GetPlayerByPeerID(int peerID)
        {
            foreach (var pair in sessionIDToPlayers)
            {
                var sessionID = pair.Key;
                var player = pair.Value;
                if (player.PeerID == peerID)
                    return player;
            }
            return null;
        }

        // TODO: Maybe add get_players_by_peer_id
        //       Maybe add get_player_names_by_peer_id

        public WebRTCPeerConnection GetWebRTCPeer(string sessionID)
        {
            if (webrtcPeers.TryGetValue(sessionID, out WebRTCPeerConnection peer))
                return peer;
            return null;
        }

        public WebRTCPeerConnection GetWebRTCPeerByPeerID(int peerID)
        {
            var player = GetPlayerByPeerID(peerID);
            if (player != null)
                return GetWebRTCPeer(player.SessionID);
            return null;
        }

        private void OnNakamaError(Exception error)
        {
            GD.Print($"{nameof(OnlineMatch)} ERROR:");
            GD.Print(error);
            Leave();
            EmitError(ErrorCode.WebsocketConnectionError, error);
        }

        private void OnNakamaClosed()
        {
            Leave();
            Disconnected?.Invoke();
        }

        private void OnNakamaMatchCreated(IMatch match)
        {
            MatchID = match.Id;
            MySessionID = match.Self.SessionId;
            Player myPlayer = Player.FromPresence(match.Self, 1);
            sessionIDToPlayers[MySessionID] = myPlayer;
            nextPeerID = 2;

            webrtcMultiplayer.Initialize(1);
            GetTree().NetworkPeer = webrtcMultiplayer;

            MatchCreated?.Invoke(MatchID);
            PlayerJoined?.Invoke(myPlayer);
            PlayerStatusChanged?.Invoke(myPlayer, PlayerStatus.Connected);
        }

        private void OnNakamaMatchPresence(IMatchPresenceEvent presence)
        {
            // Handle joining
            foreach (var user in presence.Joins)
            {
                if (user.SessionId == MySessionID) continue;
                if (MatchMode == MatchMode.Create)
                {
                    if (MatchState == MatchState.Playing)
                    {
                        // Tell this player that we've already started
                        // TODO: Maybe add feature for mid match resynchronization
                        NakamaSocket.SendMatchStateAsync(MatchID, (long)MatchOpCode.JoinError,
                            JsonSerializer.Serialize(new JoinErrorPayload()
                            {
                                Target = user.SessionId,
                                Code = JoinErrorReason.MatchHasAlreadyBegun,
                                Reason = JoinErrorMessages[JoinErrorReason.MatchHasAlreadyBegun],
                            }));
                    }
                    else if (sessionIDToPlayers.Count < MaxPlayers)
                    {
                        Player newPlayer = Player.FromPresence(user, nextPeerID);
                        nextPeerID++;
                        sessionIDToPlayers[user.SessionId] = newPlayer;
                        PlayerJoined?.Invoke(newPlayer);

                        NakamaSocket.SendMatchStateAsync(MatchID, (long)MatchOpCode.JoinSuccess,
                            JsonSerializer.Serialize(new JoinSuccessPayload()
                            {
                                Players = sessionIDToPlayers,
                                ClientVersion = ClientVersion
                            }));

                        WebRTCConnectPeer(newPlayer);
                    }
                    else
                    {
                        NakamaSocket.SendMatchStateAsync(MatchID, (long)MatchOpCode.JoinError,
                            JsonSerializer.Serialize(new JoinErrorPayload()
                            {
                                Target = user.SessionId,
                                Code = JoinErrorReason.MatchIsFull,
                                Reason = JoinErrorMessages[JoinErrorReason.MatchIsFull],
                            }));
                    }
                }
                else if (MatchMode == MatchMode.Matchmaker)
                {
                    PlayerJoined?.Invoke(sessionIDToPlayers[user.SessionId]);
                    WebRTCConnectPeer(sessionIDToPlayers[user.SessionId]);
                }
            }

            // Handle leaving
            foreach (var user in presence.Leaves)
            {
                if (user.SessionId == MySessionID || !sessionIDToPlayers.ContainsKey(user.SessionId)) continue;

                Player player = sessionIDToPlayers[user.SessionId];
                WebRTCDisconnectPeer(player);

                // If the host disconnects, this is the end!
                if (player.PeerID == 1)
                {
                    Leave();
                    EmitError(ErrorCode.HostDisconnected);
                }
                else
                {
                    sessionIDToPlayers.Remove(user.SessionId);
                    PlayerLeft?.Invoke(player);

                    if (sessionIDToPlayers.Count < MinPlayers)
                    {
                        // IF the state was previously, ready, but this brings us below the minimyum players,
                        // then we aren't ready anymore
                        if (MatchState == MatchState.Ready)
                        {
                            MatchState = MatchState.WaitingForEnoughPlayers;
                            MatchNotReady?.Invoke();
                        }
                    }
                    else
                    {
                        // If the remaining players are fully connected, then set
                        // the match state to ready
                        if (webrtcPeersConnected.Count == sessionIDToPlayers.Count - 1)
                        {
                            MatchState = MatchState.Ready;
                            MatchReady?.Invoke(sessionIDToPlayers.Values);
                        }
                    }
                }
            }
        }

        private void OnNakamaMatchJoined(IMatch match)
        {
            MatchID = match.Id;
            MySessionID = match.Self.SessionId;

            if (MatchMode == MatchMode.Join)
            {
                MatchJoined?.Invoke(MatchID);
            }
            else if (MatchMode == MatchMode.Matchmaker)
            {
                foreach (var userPresence in match.Presences)
                {
                    if (userPresence.SessionId == MySessionID) continue;
                    WebRTCConnectPeer(sessionIDToPlayers[userPresence.SessionId]);
                }
            }
        }

        private async void OnNakamaMatchmakerMatched(IMatchmakerMatched data)
        {
            // TODO: Do we need error handling for matchmaker? Where are excpetions thrown if there is an error?

            MySessionID = data.Self.Presence.SessionId;

            foreach (var user in data.Users)
                sessionIDToPlayers[user.Presence.SessionId] = Player.FromPresence(user.Presence, 0);
            var sessionIDs = new List<string>(SessionIDs);
            sessionIDs.Sort();
            foreach (var sessionID in sessionIDs)
            {
                sessionIDToPlayers[sessionID].PeerID = nextPeerID;
                nextPeerID++;
            }

            // Initialize multiplayer using our peerID
            webrtcMultiplayer.Initialize(sessionIDToPlayers[MySessionID].PeerID);
            GetTree().NetworkPeer = webrtcMultiplayer;

            MatchmakerMatched?.Invoke(Players);
            PlayerStatusChanged?.Invoke(sessionIDToPlayers[MySessionID], PlayerStatus.Connected);

            // Join the match
            try
            {
                IMatch match = await NakamaSocket.JoinMatchAsync(data);
                OnNakamaMatchJoined(match);
            }
            catch (ApiResponseException ex)
            {
                Leave();
                EmitError(ErrorCode.JoinMatchFailed, ex);
            }
        }

        private void OnNakamaMatchState(IMatchState state)
        {
            try
            {
                switch ((MatchOpCode)state.OpCode)
                {
                    case MatchOpCode.WebRTCPeerMethod:
                        // TODO
                        break;
                    case MatchOpCode.JoinSuccess:
                        var successPayload = JsonSerializer.Deserialize<JoinSuccessPayload>(Encoding.UTF8.GetString(state.State));
                        break;
                    case MatchOpCode.JoinError:
                        var errorPayload = JsonSerializer.Deserialize<JoinErrorPayload>(Encoding.UTF8.GetString(state.State));
                        break;
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is DecoderFallbackException)
            {
                return;
            }
        }

        private void OnWebRTCPeerConnected(int peerID)
        {

        }

        private void OnWebRTCPeerDisconnected(int peerID)
        {

        }

        private void WebRTCDisconnectPeer(Player player)
        {
            throw new NotImplementedException();
        }

        private void WebRTCConnectPeer(Player newPlayer)
        {
            throw new NotImplementedException();
        }
    }
}