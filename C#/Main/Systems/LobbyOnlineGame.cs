using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using NakamaWebRTC;
using System.Collections.Generic;
using System.Linq;

namespace NakamaWebRTCDemo
{
    /// <summary>
    /// Wraps around GameSession to handle Nakama online stuff
    /// like player leaving, joining, etc.
    /// Does not allow joining mid match.
    /// </summary>
    public partial class LobbyOnlineGame : Node, IOnlineGame
    {
        public enum StateType
        {
            None,
            Lobby,
            Playing
        }

        public StateType State = StateType.None;

        [OnReadyGet]
        private LobbyScreen lobbyScreen;
        [OnReadyGet]
        private GameSession gameSession;
        [OnReadyGet]
        private UILayer uiLayer;

        #region Public API
        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                ReleaseEvents();
            }
        }

        // Ran on host
        public void StartGame()
        {
            this.Rpc(nameof(StartGameEveryone));
        }

        public void MatchmakerMatched(IReadOnlyCollection<Player> players)
        {
            Reset();
            InitEvents();
            GameState.Global.OnlinePlay = true;
            State = StateType.Lobby;
            uiLayer.ShowScreen(nameof(LobbyScreen));
            // Join all the players in this match so they can be added to 
            // the gameSession and the lobbyScreen
            foreach (var player in players)
                OnPlayerJoined(player);
        }

        public void MatchCreated(string matchID)
        {
            Reset();
            InitEvents();
            GameState.Global.OnlinePlay = true;
            State = StateType.Lobby;
            uiLayer.ShowScreen(nameof(LobbyScreen), new LobbyScreen.Args()
            {
                MatchID = matchID
            });
        }

        public void MatchJoined(string matchID)
        {
            Reset();
            InitEvents();
            GameState.Global.OnlinePlay = true;
            State = StateType.Lobby;
            uiLayer.ShowScreen(nameof(LobbyScreen), new LobbyScreen.Args()
            {
                MatchID = matchID
            });
        }
        #endregion

        #region Helpers
        // Ran on everyone
        [RemoteSync]
        private void StartGameEveryone()
        {
            State = StateType.Playing;
            OnlineMatch.Global.StartPlaying();
            gameSession.StartGame();
            Console.Print("LobbyOnlineGame, Starting game");
        }

        // Ran on everyone when they first join/create a lobby
        private void Reset()
        {
            State = StateType.None;
            gameSession.Reset();
        }

        private bool EndMatchmakerMatchIfBelowMinPlayerCount()
        {
            Console.Print("Try end matchmaker match?");
            if (OnlineMatch.Global.MatchMode == MatchMode.Matchmaker && OnlineMatch.Global.IsBelowMinPlayers)
            {
                Console.Print("  Matchmaker match ended");
                // Don't bother reopening match, just end it
                gameSession.StopSession();
                return true;
            }
            return false;
        }

        private void ReopenMatch()
        {
            Console.Print("Reopening match");
            uiLayer.HideMessage();

            if (EndMatchmakerMatchIfBelowMinPlayerCount()) return;

            var args = new LobbyScreen.Args() { GameSessionPlayers = gameSession.GameSessionPlayers };
            if (OnlineMatch.Global.MatchMode != MatchMode.Matchmaker)
            {
                // Show MatchID if we're in a custom match to let more people join
                args.MatchID = OnlineMatch.Global.MatchID;
            }
                
            uiLayer.ShowScreen(nameof(LobbyScreen), args);
            foreach (var lobbyPlayer in lobbyScreen.LobbyPlayers)
                lobbyPlayer.Status = LobbyPlayerStatus.Connected;
            OnlineMatch.Global.ReopenMatch();
        }
        #endregion

        #region Event Subscriptions
        private void InitEvents()
        {
            OnlineMatch.Global.AllowJoiningMidMatch = false;
            OnlineMatch.Global.OnError += OnOnlineMatchError;
            OnlineMatch.Global.Disconnected += OnOnlineMatchDisconnected;
            OnlineMatch.Global.PlayerJoined += OnPlayerJoined;
            OnlineMatch.Global.PlayerLeft += OnPlayerLeft;
            OnlineMatch.Global.MatchReady += OnMatchReady;
            OnlineMatch.Global.MatchNotReady += OnMatchNotReady;
            OnlineMatch.Global.PlayerStatusChanged += OnPlayerStatusChanged;
            OnlineMatch.Global.OnLeave += OnLeave;

            gameSession.RoundFinished += OnRoundFinished;
            gameSession.SessionStopped += OnSessionStopped;
            lobbyScreen.ReadyButtonPressed += OnReadyButtonPressed;
        }

        private void ReleaseEvents()
        {
            if (OnlineMatch.Global != null)
            {
                OnlineMatch.Global.OnError -= OnOnlineMatchError;
                OnlineMatch.Global.Disconnected -= OnOnlineMatchDisconnected;
                OnlineMatch.Global.PlayerJoined -= OnPlayerJoined;
                OnlineMatch.Global.PlayerLeft -= OnPlayerLeft;
                OnlineMatch.Global.MatchReady -= OnMatchReady;
                OnlineMatch.Global.MatchNotReady -= OnMatchNotReady;
                OnlineMatch.Global.PlayerStatusChanged -= OnPlayerStatusChanged;
                OnlineMatch.Global.OnLeave -= OnLeave;
            }
            if (gameSession != null)
            {
                gameSession.SessionStopped -= OnSessionStopped;
                gameSession.RoundFinished -= OnRoundFinished;
                gameSession.SessionStopped -= OnSessionStopped;
            }
            if (lobbyScreen != null)
            {
                lobbyScreen.ReadyButtonPressed -= OnReadyButtonPressed;
            }
        }
        #endregion

        #region Event Handlers
        private void OnReadyButtonPressed()
        {
            Rpc(nameof(PlayerReady), GetTree().NetworkPeer.GetUniqueId());
        }

        // Called on everyone
        [RemoteSync]
        private void PlayerReady(int peerID)
        {
            var lobbyPlayer = lobbyScreen.GetLobbyPlayer(peerID);
            lobbyPlayer.Status = LobbyPlayerStatus.Readied;

            ServerStartIfReady();
        }

        private bool ServerStartIfReady()
        {
            // As host, start the game if everyone is readied
            if (GetTree().IsNetworkServer() && lobbyScreen.LobbyPlayers.All(x => x.Status == LobbyPlayerStatus.Readied))
            {
                StartGame();
                return true;
            }
            return false;
        }

        // Called on everyone
        private void OnRoundFinished(bool isMatchOver)
        {
            if (isMatchOver)
            {
                gameSession.StopSession();
                return;
            }

            Console.Print("Setting state to lobby");
            State = StateType.Lobby;

            // Reopen match once the round is finished. It's easier to let people join the lobby
            // once the round is done, since everyone will be back in the lobby.
            ReopenMatch();
        }


        private async void OnSessionStopped()
        {
            await OnlineMatch.Global.Leave();
            uiLayer.ShowScreen(nameof(MatchScreen));
        }

        // Leave should reset everything
        private void OnLeave()
        {
            GameState.Global.OnlinePlay = false;
            ReleaseEvents();
            Reset();
        }

        // Disconnected is called after leave, once the nakama socket is closed in leave
        private void OnOnlineMatchDisconnected()
        {
            uiLayer.ShowScreen(nameof(MatchScreen));
        }

        private void OnOnlineMatchError(string message)
        {
            // Kick the user back to the MatchScreen if we get an error
            if (message != "")
                uiLayer.ShowMessage(message, 2f);
            uiLayer.ShowScreen(nameof(MatchScreen));
        }

        private void OnPlayerJoined(Player player)
        {
            GD.Print(nameof(OnPlayerJoined) + ": " + player.PeerID);
            var newSessionPlayer = gameSession.AddPlayer(player);
            if (newSessionPlayer == null)
                return;
            lobbyScreen.AddLobbyPlayer(newSessionPlayer);
        }

        private void OnPlayerLeft(Player player)
        {
            uiLayer.ShowMessage(player.Username + " has left", 2f);

            gameSession.RemovePlayer(player);
            lobbyScreen.RemovePlayer(player);

            if (State == StateType.Lobby)
            {
                if (EndMatchmakerMatchIfBelowMinPlayerCount()) return;
                if (ServerStartIfReady()) return;
            }
        }

        private void OnMatchNotReady() => lobbyScreen.SetReadyButtonEnabled(false);

        private void OnMatchReady(IReadOnlyCollection<Player> players) => lobbyScreen.SetReadyButtonEnabled(true);

        private void OnPlayerStatusChanged(Player player, PlayerStatus status)
        {
            var lobbyPlayer = lobbyScreen.GetLobbyPlayer(player);
            if (status == PlayerStatus.Connected)
            {
                // Note that we might get a race condition where
                // the WebRTC connections are conected first, before
                // Nakama's player status change messages has had the
                // chance to arrive.

                // If WebRTC connection is made (leading to READY!),
                // we keep it as ready, since all players are connected.
                if (lobbyPlayer.Status != LobbyPlayerStatus.Readied)
                    lobbyPlayer.Status = LobbyPlayerStatus.Connected;

                if (GetTree().IsNetworkServer())
                {
                    // As host, notify player about readied players
                    foreach (var currLobbyPlayer in lobbyScreen.LobbyPlayers)
                    {
                        if (currLobbyPlayer.Status == LobbyPlayerStatus.Readied)
                            RpcId(player.PeerID, nameof(PlayerReady), currLobbyPlayer.SessionPlayer.Player.PeerID);
                    }
                    // As host, notify player about existing GameSessionPlayers
                    RpcId(player.PeerID, nameof(SyncMatchData), gameSession.Serialize());
                }
            }
            else if (status == PlayerStatus.Connecting)
            {
                lobbyPlayer.Status = LobbyPlayerStatus.Connecting;
            }
        }
        
        [RemoteSync]
        private void SyncMatchData(byte[] bytes)
        {
            gameSession.Deserialize(bytes.ToBuffer());
            lobbyScreen.UpdateLobbyPlayerDisplays();
        }
        #endregion
    }
}