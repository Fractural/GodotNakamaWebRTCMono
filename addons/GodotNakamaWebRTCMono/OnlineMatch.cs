using Godot;
using Nakama;
using System;
using System.Collections.Generic;
using GDC = Godot.Collections;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace NakamaWebRTC
{
    #region Enums & Helper Classes
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
        public GDC.Dictionary ToGDDict()
        {
            return new
            {
                urls = Urls
            }.ToGDDict();
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

    public class Player : IBufferSerializable
    {
        public string SessionID { get; set; }
        public string Username { get; set; }
        public int PeerID { get; set; }

        public Player() { }

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

        public static Player FromLocal(string username, int peerID)
        {
            // We don't use sessionID if the player is local
            return new Player("", username, peerID);
        }

        public void Serialize(StreamPeerBuffer buffer)
        {
            buffer.PutString(SessionID);
            buffer.PutString(Username);
            buffer.Put32(PeerID);
        }

        public void Deserialize(StreamPeerBuffer buffer)
        {
            SessionID = buffer.GetString();
            Username = buffer.GetString();
            PeerID = buffer.Get32();
        }
    }
    #endregion

    public class OnlineMatch : Node
    {
        public static OnlineMatch Global { get; private set; }

        #region Settings
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
        #endregion

        #region Vars
        #region Nakama Vars
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
        public int MyPeerID => GetTree().NetworkPeer?.GetUniqueId() ?? -1;
        public string MySessionID { get; private set; }
        public string MatchID { get; private set; }
        public IMatchmakerTicket MatchmakerTicket { get; private set; }
        #endregion

        #region WebRTC Vars
        private WebRTCMultiplayer webrtcMultiplayer;
        private Dictionary<string, WebRTCPeerConnection> webrtcPeers = new Dictionary<string, WebRTCPeerConnection>();
        private Dictionary<string, bool> webrtcPeersConnected = new Dictionary<string, bool>();
        #endregion

        #region Player Vars
        /// <summary>
        /// Session ID to Player dictionary
        /// </summary>
        private Dictionary<string, Player> sessionIDToPlayers = new Dictionary<string, Player>();
        private int nextPeerID;
        #endregion

        #region Readonly Vars
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
        public IReadOnlyCollection<string> SessionIDs => sessionIDToPlayers.Keys;
        public IReadOnlyCollection<Player> Players => sessionIDToPlayers.Values;
        #endregion
        #endregion

        #region Events
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
        public event Action<IReadOnlyCollection<Player>> MatchmakerMatched;

        // Player player
        public event Action<Player> PlayerJoined;
        // Player player
        public event Action<Player> PlayerLeft;
        // Player player, PlayerStatus status
        public event Action<Player, PlayerStatus> PlayerStatusChanged;

        // IEnumerable<Player> Players
        public event Action<IReadOnlyCollection<Player>> MatchReady;
        public event Action MatchNotReady;

        // WebRTCPeerConnection webrtcPeer, Player player
        public event Action<WebRTCPeerConnection, Player> WebRTCPeerAdded;
        // WebRTCPeerConnection webrtcPeer, Player player
        public event Action<WebRTCPeerConnection, Player> WebRTCPeerRemoved;
        #endregion

        #region Match State Payloads
        public class WebRTCPeerMethodPayload : IBufferSerializable
        {

            public enum MethodType
            {
                SetRemoteDescription,
                AddIceCandidate,
                Reconnect
            }

            public MethodType Method { get; set; }

            /// <summary>
            /// Session ID of target player
            /// </summary>
            public string Target { get; set; }
            public object[] Arguments { get; set; } = null;

            public T GetArg<T>(int index)
            {
                return (T)Arguments[index];
            }

            public void Serialize(StreamPeerBuffer buffer)
            {
                buffer.PutU8((byte)Method);
                buffer.PutString(Target);
                buffer.PutU8((byte)Arguments.Length);
                foreach (var arg in Arguments)
                {
                    TypeCode typeCode = Type.GetTypeCode(arg.GetType());
                    buffer.PutU8((byte)typeCode);
                    switch (typeCode)
                    {
                        case TypeCode.Byte:
                            buffer.PutU8((byte)arg);
                            break;
                        case TypeCode.Int32:
                            buffer.Put32((int)arg);
                            break;
                        case TypeCode.Int64:
                            buffer.Put64((long)arg);
                            break;
                        case TypeCode.Single:
                            buffer.PutFloat((float)arg);
                            break;
                        case TypeCode.Double:
                            buffer.PutDouble((double)arg);
                            break;
                        case TypeCode.Char:
                            buffer.PutString(arg.ToString());
                            break;
                        case TypeCode.Boolean:
                            buffer.PutU8(Convert.ToByte(arg));
                            break;
                        case TypeCode.String:
                            buffer.PutString((string)arg);
                            break;
                        default:
                            throw new Exception($"{nameof(WebRTCPeerMethodPayload)}: Cannot serialize argument with unhandled type code \"{typeCode}\"");
                    }
                }

            }

            public void Deserialize(StreamPeerBuffer buffer)
            {
                Method = (MethodType)buffer.GetU8();
                Target = buffer.GetString();
                int argCount = buffer.GetU8();
                Arguments = new object[argCount];
                for (int i = 0; i < argCount; i++)
                {
                    TypeCode typeCode = (TypeCode)buffer.GetU8();
                    switch (typeCode)
                    {
                        case TypeCode.Byte:
                            Arguments[i] = buffer.GetU8();
                            break;
                        case TypeCode.Int32:
                            Arguments[i] = buffer.Get32();
                            break;
                        case TypeCode.Int64:
                            Arguments[i] = buffer.Get64();
                            break;
                        case TypeCode.Single:
                            Arguments[i] = buffer.GetFloat();
                            break;
                        case TypeCode.Double:
                            Arguments[i] = buffer.GetDouble();
                            break;
                        case TypeCode.Char:
                            Arguments[i] = buffer.GetString()[0];
                            break;
                        case TypeCode.Boolean:
                            Arguments[i] = Convert.ToBoolean(buffer.GetU8());
                            break;
                        case TypeCode.String:
                            Arguments[i] = buffer.GetString();
                            break;
                        default:
                            throw new Exception($"{nameof(WebRTCPeerMethodPayload)}: Cannot deserialize argument with unhandled type code \"{typeCode}\"");
                    }
                }
            }
        }

        public class JoinSuccessPayload : IBufferSerializable
        {
            /// <summary>
            /// Session ID to player
            /// </summary>
            public IReadOnlyCollection<Player> Players { get; set; }
            public string HostClientVersion { get; set; }

            public void Serialize(StreamPeerBuffer buffer)
            {
                buffer.Put32(Players.Count);
                foreach (var player in Players)
                    player.Serialize(buffer);
                buffer.PutString(HostClientVersion);
            }

            public void Deserialize(StreamPeerBuffer buffer)
            {
                var playersList = new List<Player>();

                int playerCount = buffer.Get32();
                for (int i = 0; i < playerCount; i++)
                {
                    var player = new Player();
                    player.Deserialize(buffer);
                    playersList.Add(player);
                }

                Players = playersList;
                HostClientVersion = buffer.GetString();
            }
        }

        public class JoinErrorPayload : IBufferSerializable
        {
            /// <summary>
            /// Target player's session ID
            /// </summary>
            public string Target { get; set; }
            public JoinErrorReason Code { get; set; }
            public string Reason { get; set; }

            public void Serialize(StreamPeerBuffer buffer)
            {
                buffer.PutString(Target);
                buffer.Put32((int)Code);
                buffer.PutString(Reason);
            }

            public void Deserialize(StreamPeerBuffer buffer)
            {
                Target = buffer.GetString();
                Code = (JoinErrorReason)buffer.Get32();
                Reason = buffer.GetString();
            }
        }
        #endregion

        #region Public API
        public override void _Ready()
        {
            if (Global != null)
            {
                QueueFree();
                return;
            }
            Global = this;

            var webrtcPeer = new WebRTCPeerConnection();
            webrtcPeer.Initialize();
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                if (Global == this)
                    Global = null;
            }
        }

        public async void CreateMatch(ISocket nakamaSocket)
        {
            await Leave();
            NakamaSocket = nakamaSocket;

            try
            {
                IMatch match = await nakamaSocket.CreateMatchAsync();
                OnNakamaMatchCreated(match);
            }
            catch (WebSocketException ex)
            {
                await Leave();
                EmitError(ErrorCode.MatchCreateFailed, ex);
                // TODO: Maybe remove emit error altogether if error is already handled by the exception itself
            }
        }

        public async void JoinMatch(ISocket nakamaSocket, string matchID)
        {
            await Leave();
            NakamaSocket = nakamaSocket;
            MatchMode = MatchMode.Join;

            try
            {
                IMatch match = await NakamaSocket.JoinMatchAsync(matchID);
                OnNakamaMatchJoined(match);
            }
            catch (WebSocketException ex)
            {
                await Leave();
                EmitError(ErrorCode.JoinMatchFailed, ex);
            }
        }

        public async void StartMatchmaking(ISocket nakamaSocket, MatchmakingArgs args = null)
        {
            await Leave();
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
            catch (WebSocketException ex)
            {
                await Leave();
                EmitError(ErrorCode.StartMatchmakingFailed, ex);
            }
        }

        public void StartPlaying()
        {
            Debug.Assert(MatchState == MatchState.Ready);
            MatchState = MatchState.Playing;
        }

        public async Task Leave(bool closeSocket = false)
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
            webrtcPeers.Clear();
            webrtcPeersConnected.Clear();
            sessionIDToPlayers.Clear();
            nextPeerID = 1;
            MatchState = MatchState.Lobby;
            MatchMode = MatchMode.None;
        }
        #endregion

        #region Helper Methods
        private void EmitError(ErrorCode code, object extra = null)
        {
            string message = ErrorMessages[code];
            if (code == ErrorCode.ClientJoinError)
                message = JoinErrorMessages[(JoinErrorReason)extra];
            OnError?.Invoke(message);
            OnErrorCode?.Invoke(code, message, extra);
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
        #endregion

        #region Nakama
        private async void OnNakamaError(Exception error)
        {
            GD.Print($"{nameof(OnlineMatch)} ERROR:");
            GD.Print(error);
            await Leave();
            EmitError(ErrorCode.WebsocketConnectionError, error);
        }

        private async void OnNakamaClosed()
        {
            await Leave();
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

        private async void OnNakamaMatchPresence(IMatchPresenceEvent presence)
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
                        await NakamaSocket.SendMatchStateAsync(MatchID, (long)MatchOpCode.JoinError,
                            new JoinErrorPayload()
                            {
                                Target = user.SessionId,
                                Code = JoinErrorReason.MatchHasAlreadyBegun,
                                Reason = JoinErrorMessages[JoinErrorReason.MatchHasAlreadyBegun],
                            }.Serialize());
                    }
                    else if (sessionIDToPlayers.Count < MaxPlayers)
                    {
                        Player newPlayer = Player.FromPresence(user, nextPeerID);
                        nextPeerID++;
                        sessionIDToPlayers[user.SessionId] = newPlayer;
                        PlayerJoined?.Invoke(newPlayer);

                        await NakamaSocket.SendMatchStateAsync(MatchID, (long)MatchOpCode.JoinSuccess,
                            new JoinSuccessPayload()
                            {
                                Players = Players,
                                HostClientVersion = ClientVersion
                            }.Serialize());

                        WebRTCConnectPeer(newPlayer);
                    }
                    else
                    {
                        await NakamaSocket.SendMatchStateAsync(MatchID, (long)MatchOpCode.JoinError,
                            new JoinErrorPayload()
                            {
                                Target = user.SessionId,
                                Code = JoinErrorReason.MatchIsFull,
                                Reason = JoinErrorMessages[JoinErrorReason.MatchIsFull],
                            }.Serialize());
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
                    await Leave();
                    EmitError(ErrorCode.HostDisconnected);
                }
                else
                {
                    sessionIDToPlayers.Remove(user.SessionId);
                    PlayerLeft?.Invoke(player);

                    if (sessionIDToPlayers.Count < MinPlayers)
                    {
                        // IF the state was previously, ready, but this brings us below the minimum players,
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
            foreach (var sessionID in SessionIDs.OrderBy(x => x))
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
            catch (WebSocketException ex)
            {
                await Leave();
                EmitError(ErrorCode.JoinMatchFailed, ex);
            }
        }

        #region Nakama Match State
        private void OnNakamaMatchState(IMatchState state)
        {
            try
            {
                switch ((MatchOpCode)state.OpCode)
                {
                    case MatchOpCode.WebRTCPeerMethod:
                        HandleWebRTCPeerMethod(state);
                        break;
                    case MatchOpCode.JoinSuccess:
                        HandleJoinSuccess(state);
                        break;
                    case MatchOpCode.JoinError:
                        HandleJoinError(state);
                        break;
                }
            }
            catch (Exception ex)
            {
                GD.Print($"{nameof(OnlineMatch)}: Encountered error when handling Nakama Match state:\n\t{ex}");
                return;
            }
        }

        private void HandleWebRTCPeerMethod(IMatchState state)
        {
            var payload = state.State.Deserialize<WebRTCPeerMethodPayload>();

            // We're using Nakama to do RPC calls on peers to set up the WebRTC connection.
            // Once the WebRTC connection is up, then we'll be able to use the usual
            // Godot networking calls.
            if (payload.Target == MySessionID)
            {
                string sessionID = state.UserPresence.SessionId;
                if (!webrtcPeers.ContainsKey(sessionID))
                    return;
                var webrtcPeer = webrtcPeers[sessionID];
                switch (payload.Method)
                {
                    case WebRTCPeerMethodPayload.MethodType.SetRemoteDescription:
                        webrtcPeer.SetRemoteDescription(
                            payload.GetArg<string>(0),
                            payload.GetArg<string>(1));
                        break;
                    case WebRTCPeerMethodPayload.MethodType.AddIceCandidate:
                        string name = payload.GetArg<string>(2);
                        if (WebRTCCheckIceCandidate(name))
                            webrtcPeer.AddIceCandidate(
                                payload.GetArg<string>(0),
                                payload.GetArg<int>(1),
                                payload.GetArg<string>(2));
                        break;
                    case WebRTCPeerMethodPayload.MethodType.Reconnect:
                        var player = sessionIDToPlayers[sessionID];
                        webrtcMultiplayer.RemovePeer(player.PeerID);
                        WebRTCReconnectPeer(player);
                        break;
                }
            }
        }

        private async void HandleJoinSuccess(IMatchState state)
        {
            if (MatchMode != MatchMode.Join)
                return;

            var payload = state.State.Deserialize<JoinSuccessPayload>();
            if (ClientVersion != payload.HostClientVersion)
            {
                await Leave();
                EmitError(ErrorCode.ClientVersionError, payload.HostClientVersion);
            }

            // Add any players that we're missing from the payload of players
            foreach (var player in payload.Players)
            {
                if (!sessionIDToPlayers.ContainsKey(player.SessionID))
                {
                    sessionIDToPlayers[player.SessionID] = player;
                    WebRTCConnectPeer(player);
                    PlayerJoined?.Invoke(player);
                    if (player.SessionID == MySessionID)
                    {
                        webrtcMultiplayer.Initialize(player.PeerID);
                        GetTree().NetworkPeer = webrtcMultiplayer;
                        PlayerStatusChanged?.Invoke(player, PlayerStatus.Connected);
                    }
                }
            }
        }

        private async void HandleJoinError(IMatchState state)
        {
            var payload = state.State.Deserialize<JoinErrorPayload>();

            if (payload.Target == MySessionID)
            {
                await Leave();
                EmitError(ErrorCode.ClientJoinError, payload.Code);
            }
        }
        #endregion
        #endregion

        #region WebRTC
        private void WebRTCConnectPeer(Player player)
        {
            // Don't add the same player twice
            if (webrtcPeers.ContainsKey(player.SessionID))
                return;

            // If the match was previously ready, then we need to switch it back to not ready
            if (MatchState == MatchState.Ready)
                MatchNotReady?.Invoke();

            // If we're already playing, then this is a reconnect attempt, so don't mess with the state
            // Otherwise, change state to connecting becasue we're trying to connect to all peers
            if (MatchState != MatchState.Playing)
                MatchState = MatchState.Connecting;

            var webrtcPeer = new WebRTCPeerConnection();
            webrtcPeer.Initialize(new
            {
                iceServers = IceServersConfig.ToGDDict()
            }.ToGDDict());

            webrtcPeer.Connect("session_description_created", this, nameof(OnWebRTCPeerSessionDescriptionCreated), Utils.GDParams(player.SessionID));
            webrtcPeer.Connect("ice_candidate_created", this, nameof(OnWebRTCPeerIceCandidateCreated), Utils.GDParams(player.SessionID));

            webrtcPeers[player.SessionID] = webrtcPeer;

            webrtcMultiplayer.AddPeer(webrtcPeer, player.PeerID, 0);

            WebRTCPeerAdded?.Invoke(webrtcPeer, player);

            if (MySessionID.CasecmpTo(player.SessionID) < 0)
            {
                var result = webrtcPeer.CreateOffer();
                if (result != Error.Ok)
                    EmitError(ErrorCode.WebRTCOfferError, result);
            }

        }

        private void WebRTCDisconnectPeer(Player player)
        {
            var webrtcPeer = webrtcPeers[player.SessionID];
            WebRTCPeerRemoved?.Invoke(webrtcPeer, player);
            webrtcPeer.Close();
            webrtcPeers.Remove(player.SessionID);
        }

        private void WebRTCReconnectPeer(Player player)
        {
            if (webrtcPeers.TryGetValue(player.SessionID, out WebRTCPeerConnection oldWebRTCPeer))
            {
                WebRTCPeerRemoved?.Invoke(oldWebRTCPeer, player);
                oldWebRTCPeer.Close();
            }

            webrtcPeersConnected.Remove(player.SessionID);
            webrtcPeers.Remove(player.SessionID);

            GD.Print($"{nameof(OnlineMatch)}: Starting WebRTC reconnect...");

            WebRTCConnectPeer(player);

            PlayerStatusChanged?.Invoke(player, PlayerStatus.Connecting);

            if (MatchState == MatchState.Ready)
            {
                MatchState = MatchState.Connecting;
                MatchNotReady?.Invoke();
            }
        }

        private void OnWebRTCPeerSessionDescriptionCreated(string type, string sdp, string sessionID)
        {
            var webrtcPeer = webrtcPeers[sessionID];
            webrtcPeer.SetLocalDescription(type, sdp);

            // Send this data to the peer so they can call SetRemoteDescription
            nakamaSocket.SendMatchStateAsync(MatchID, (long)MatchOpCode.WebRTCPeerMethod,
                new WebRTCPeerMethodPayload()
                {
                    Method = WebRTCPeerMethodPayload.MethodType.SetRemoteDescription,
                    Target = sessionID,
                    Arguments = Utils.Params(type, sdp)
                }.Serialize());
        }

        private bool WebRTCCheckIceCandidate(string name)
        {
            if (UseNetworkRelay == NetworkRelay.Auto)
                return true;

            bool isRelay = name.Contains("typ relay");

            // UseNetworkRelay = NetworkRelay.Forced, so we must detect the relay to verify ice candidate
            if (UseNetworkRelay == NetworkRelay.Forced)
                return isRelay;
            // UseNetworkRelay = NetworkRelay.Disabled, so we only verify ice candidate if it doesn't have a relay
            return !isRelay;
        }

        private void OnWebRTCPeerIceCandidateCreated(string media, int index, string name, string sessionID)
        {
            if (!WebRTCCheckIceCandidate(name))
                return;

            // Send this data to the peer so they can call .AddIceCandidate
            nakamaSocket.SendMatchStateAsync(MatchID, (long)MatchOpCode.WebRTCPeerMethod,
                new WebRTCPeerMethodPayload()
                {
                    Method = WebRTCPeerMethodPayload.MethodType.AddIceCandidate,
                    Target = sessionID,
                    Arguments = Utils.Params(media, index, name)
                }.Serialize());
        }

        private void OnWebRTCPeerConnected(int peerID)
        {
            foreach (var player in Players)
            {
                if (player.PeerID == peerID)
                {
                    webrtcPeersConnected[player.SessionID] = true;
                    GD.Print($"{nameof(OnlineMatch)}: WebRTC peer connected: " + peerID);
                    PlayerStatusChanged?.Invoke(player, PlayerStatus.Connected);
                }
            }

            // We have a WebRTC peer for each connection to another player, so we'll have one less
            // than the number of players (ie. no peer connection to ourselves)
            if (webrtcPeersConnected.Count == Players.Count - 1)
            {
                if (Players.Count >= MinPlayers)
                {
                    // All our peers are good, so we can assume godot RPC will work now
                    MatchState = MatchState.Ready;
                    MatchReady?.Invoke(Players);
                }
                else
                {
                    MatchState = MatchState.WaitingForEnoughPlayers;
                }
            }
        }

        // If we loose a WebRTC connection, we try to reconnect
        private void OnWebRTCPeerDisconnected(int peerID)
        {
            GD.Print($"{nameof(OnlineMatch)}: WebRTC peer disconnected: " + peerID);

            foreach (var player in Players)
            {
                // We only intiate reconnection process from only one side (the offer side)
                if (player.PeerID == peerID && MySessionID.CasecmpTo(player.SessionID) < 0)
                {
                    nakamaSocket.SendMatchStateAsync(MatchID, (long)MatchOpCode.WebRTCPeerMethod,
                        new WebRTCPeerMethodPayload()
                        {
                            Method = WebRTCPeerMethodPayload.MethodType.Reconnect,
                            Target = player.SessionID
                        }.Serialize());

                    WebRTCReconnectPeer(player);
                }
            }
        }
        #endregion
    }
}