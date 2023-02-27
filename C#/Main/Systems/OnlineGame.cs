using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using NakamaWebRTC;
using System;
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
        private bool hasGameStarted = false;

        [OnReady]
        public void RealReady()
        {
            OnlineMatch.Global.OnError += OnOnlineMatchError;
            OnlineMatch.Global.Disconnected += OnOnlineMatchDisconnected;
            OnlineMatch.Global.PlayerLeft += OnOnlineMatchPlayerLeft;
            OnlineMatch.Global.OnLeave += OnLeave;
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
                    OnlineMatch.Global.OnLeave -= OnLeave;
                }
            }
        }

        // Ran on host
        public void StartGame()
        {
            this.TryRpc(nameof(StartGameEveryone));
        }

        // Ran on everyone
        [RemoteSync]
        private void StartGameEveryone()
        {
            if (!hasGameStarted)
            {
                hasGameStarted = true;
                OnlineMatch.Global.StartPlaying();
                gameSession.StartSession(new List<Player>(OnlineMatch.Global.Players));
            }
            else
                gameSession.StartSession();
        }

        private void OnLeave()
        {
            GameState.Global.OnlinePlay = false;
            Reset();
        }

        // Ran on everyone when they first join/create a lobby
        public void Reset()
        {
            hasGameStarted = false;
            gameSession.Reset();
        }

        private void OnOnlineMatchPlayerLeft(Player player)
        {
            uiLayer.ShowMessage(player.Username + " has left", 2f);

            gameSession.RemovePlayer(player);

            // If we are below min players, then reopen the match after the results are shown
            if (OnlineMatch.Global.IsBelowMinPlayers)
            {
                if (OnlineMatch.Global.MatchMode == MatchMode.Matchmaker)
                    // Leave match
                    gameSession.RoundOverActionOverride = OnOnlineMatchDisconnected;
                else
                    // Reopen match if we're in a private lobby
                    gameSession.RoundOverActionOverride = ReopenMatch;
                // TODO NOW:
                //  Separate the match lobby from the session lobby
                //  Right now the lobby is doing double duty as both a lobby
                //  for people to join and a lobby for existing players to ready up
                //  in between matches. This is really hacky, and since the lobby calls
                //  OnlineGame.StartGame() eaach time, OnlineGame then has to handle 
                //  the lifecycle of the game (detecting if the game's started) rather
                //  than GameSession.
            }
        }

        private void ReopenMatch()
        {
            Reset();
            uiLayer.ShowScreen(nameof(LobbyScreen), new LobbyScreen.Args()
            {
                MatchID = OnlineMatch.Global.MatchID
            });
            OnlineMatch.Global.ReopenMatch();
        }

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
    }
}
