using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using NakamaWebRTC;
using System.Collections.Generic;

namespace NakamaWebRTCDemo
{
    /// <summary>
    /// Wraps around GameSession to handle Nakama online stuff
    /// like player leaving, joining, etc.
    /// </summary>
    public partial class OnlineGame : Node
    {
        [OnReadyGet]
        private GameSession gameSession;
        [OnReadyGet]
        private UILayer uiLayer;

        public List<Player> Players = new List<Player>();

        [OnReady]
        public void RealReady()
        {
            OnlineMatch.Global.OnError += OnOnlineMatchError;
            OnlineMatch.Global.Disconnected += OnOnlineMatchDisconnected;
            OnlineMatch.Global.PlayerLeft += OnOnlineMatchPlayerLeft;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                if (OnlineMatch.Global != null)
                {
                    OnlineMatch.Global.OnError -= OnOnlineMatchError;
                    OnlineMatch.Global.Disconnected -= OnOnlineMatchDisconnected;
                    OnlineMatch.Global.PlayerLeft -= OnOnlineMatchPlayerLeft;
                }
            }
        }

        public void LoadAndStartGame()
        {
            GameState.Global.OnlinePlay = true;
            Players = new List<Player>(OnlineMatch.Global.Players);

            gameSession.LoadAndStartSession(Players);
        }

        private void OnOnlineMatchPlayerLeft(Player player)
        {
            uiLayer.ShowMessage(player.Username + " has left", 2f);

            gameSession.RemovePlayer(player);

            Players.Remove(player);
        }

        private void OnOnlineMatchDisconnected()
        {
            uiLayer.ShowScreen("MatchScreen");
        }

        private void OnOnlineMatchError(string message)
        {
            // Kick the user back to the MatchScreen if we get an error
            if (message != "")
                uiLayer.ShowMessage(message, 2f);
            uiLayer.ShowScreen("MatchScreen");
        }
    }
}
