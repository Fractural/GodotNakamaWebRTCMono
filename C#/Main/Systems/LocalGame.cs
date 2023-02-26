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

        /// <summary>
        /// Starts a local game.
        /// </summary>
        public void LoadAndStartGame()
        {
            var players = new List<Player>()
            {
                Player.FromLocal("Player1", 1),
                Player.FromLocal("Player2", 2),
            };
            gameSession.LoadAndStartSession(players);
        }
    }
}
