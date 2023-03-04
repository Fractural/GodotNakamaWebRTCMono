using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using NakamaWebRTC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NakamaWebRTCDemo
{
    public partial class LobbyScreen : Screen
    {
        public class Args
        {
            public IReadOnlyCollection<Player> Players { get; set; }
            public IReadOnlyCollection<GameSessionPlayer> GameSessionPlayers { get; set; }
            public string MatchID { get; set; }
        }

        public List<LobbyPlayer> LobbyPlayers { get; private set; } = new List<LobbyPlayer>();

        [OnReadyGet]
        private Button readyButton;
        [OnReadyGet]
        private Control matchIDContainer;
        [OnReadyGet]
        private LineEdit matchIDLineEdit;
        [OnReadyGet]
        private Control lobbyPlayerContainer;
        [OnReadyGet]
        private Button copyMatchIDButton;
        [Export]
        private PackedScene lobbyPlayerPrefab;

        public event Action ReadyButtonPressed;

        [OnReady]
        public void RealReady()
        {
            ClearLobbyPlayers();

            readyButton.Connect("pressed", this, nameof(OnReadyButtonPressed));
            copyMatchIDButton.Connect("pressed", this, nameof(OnCopyMatchIDButtonPressed));
        }

        public override void ShowScreen(object args)
        {
            base.ShowScreen(args);

            // Back button returns to match screen and also leaves match
            uiLayer.BackButtonActionOverride = async () =>
            {
                uiLayer.ShowScreen(nameof(MatchScreen));
                await OnlineMatch.Global.Leave();
            };

            IReadOnlyCollection<Player> players = new Player[0];
            string matchID = "";

            Args castedArgs = null;
            if (args is Args)
            {
                castedArgs = (Args)args;
                if (castedArgs.Players != null)
                    players = castedArgs.Players;
                if (castedArgs.MatchID != null)
                    matchID = castedArgs.MatchID;
            }

            ClearLobbyPlayers();

            if (castedArgs?.GameSessionPlayers != null)
            {
                // We have come here as an intermission.
                // Add game session players.
                foreach (var sessionPlayer in castedArgs.GameSessionPlayers)
                    AddLobbyPlayer(sessionPlayer);
                // Let people ready themselves up -- No need to
                // wait for MatchReady because everyone is already
                // connected to each other.
                SetReadyButtonEnabled(true);
            }
            else
            {
                // We've made a game/joined a game.
                // Add players that are already in the lobby if any.
                foreach (var player in players)
                    AddLobbyPlayer(player);
                // Disable the ready button by default until OnlineMatch
                // tells us everyone is connected via WebRTC.
                SetReadyButtonEnabled(false);
            }

            // NOTE: MatchID is only passed in when
            // the room is first made. During 
            // intermissions it will not appear
            // because the match has already started.
            if (matchID != "")
            {
                matchIDContainer.Visible = true;
                matchIDLineEdit.Text = matchID;
            }
            else
            {
                matchIDContainer.Visible = false;
            }

            readyButton.GrabFocus();
        }

        private void ClearLobbyPlayers()
        {
            foreach (var lobbyPlayer in LobbyPlayers)
                lobbyPlayer.QueueFree();
            LobbyPlayers.Clear();
            readyButton.Disabled = true;
        }

        public LobbyPlayer AddLobbyPlayer(GameSessionPlayer sessionPlayer)
        {
            if (!LobbyPlayers.Any(x => x.Player == sessionPlayer.Player))
            {
                LobbyPlayer lobbyPlayer = lobbyPlayerPrefab.Instance<LobbyPlayer>();
                lobbyPlayerContainer.AddChild(lobbyPlayer);
                lobbyPlayer.Construct(sessionPlayer.Player, LobbyPlayerStatus.Waiting, sessionPlayer.Score);
                LobbyPlayers.Add(lobbyPlayer);
                return lobbyPlayer;
            }
            return null;
        }

        public LobbyPlayer AddLobbyPlayer(Player player)
        {
            if (!LobbyPlayers.Any(x => x.Player == player))
            {
                LobbyPlayer lobbyPlayer = lobbyPlayerPrefab.Instance<LobbyPlayer>();
                lobbyPlayerContainer.AddChild(lobbyPlayer);
                lobbyPlayer.Construct(player, LobbyPlayerStatus.Connecting);
                LobbyPlayers.Add(lobbyPlayer);
                return lobbyPlayer;
            }
            return null;
        }

        public void RemovePlayer(Player player)
        {
            LobbyPlayer lobbyPlayer = LobbyPlayers.Find(x => x.Player == player);
            if (lobbyPlayer != null)
            {
                lobbyPlayer.QueueFree();
                LobbyPlayers.Remove(lobbyPlayer);
            }
        }

        public LobbyPlayer GetLobbyPlayer(int peerID)
        {
            return LobbyPlayers.Find(x => x.Player.PeerID == peerID);
        }

        public LobbyPlayer GetLobbyPlayer(Player player)
        {
            return LobbyPlayers.Find(x => x.Player == player);
        }

        public void SetReadyButtonEnabled(bool enabled)
        {
            readyButton.Disabled = !enabled;
            if (enabled)
                readyButton.GrabFocus();
        }

        private void OnReadyButtonPressed()
        {
            ReadyButtonPressed?.Invoke();
        }

        private void OnCopyMatchIDButtonPressed()
        {
            OS.Clipboard = matchIDLineEdit.Text;
        }
    }
}
