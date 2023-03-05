using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using NakamaWebRTC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NakamaWebRTCDemo
{
    /// <summary>
    /// Wraps around GameSession. Runs the session
    /// locally.
    /// </summary>
    public partial class LocalGame : Node
    {
        [OnReadyGet]
        private GameSession gameSession;
        [OnReadyGet]
        private UILayer uiLayer;

        private void InitEvents()
        {
            gameSession.SessionStopped += OnSessionStopped;
            gameSession.RoundFinished += OnRoundFinished;
        }

        private void ReleaseEvents()
        {
            gameSession.SessionStopped -= OnSessionStopped;
            gameSession.RoundFinished -= OnRoundFinished;
        }

        private void OnRoundFinished(bool isMatchOver)
        {
            if (!isMatchOver)
                gameSession.RestartGame();
        }

        private void OnSessionStopped()
        {
            uiLayer.ShowScreen(nameof(TitleScreen));
            ReleaseEvents();
        }

        /// <summary>
        /// Starts a local game.
        /// </summary>
        public void StartGame()
        {
            InitEvents();

            GameState.Global.OnlinePlay = false;
            var players = new List<Player>()
            {
                Player.FromLocal("Player1", 1),
                Player.FromLocal("Player2", 2),
            };
            gameSession.AddPlayers(players);
            gameSession.StartGame();
        }
    }
}
