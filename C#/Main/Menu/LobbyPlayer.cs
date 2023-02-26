using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using NakamaWebRTC;
using System;
using System.Collections.Generic;

namespace NakamaWebRTCDemo
{
    public enum LobbyPlayerStatus
    {
        Waiting,
        Connecting,
        Connected,
        Readied
    }

    /// <summary>
    /// Represents a player in a lobby. Contains the lobby status of the 
    /// player as well as the UI related to a lobby player.
    /// </summary>
    public partial class LobbyPlayer : Control
    {
        public static readonly Dictionary<LobbyPlayerStatus, string> LobbyPlayerStatusMessages = new Dictionary<LobbyPlayerStatus, string>()
        {
            [LobbyPlayerStatus.Waiting] = "Waiting...",
            [LobbyPlayerStatus.Connecting] = "Connecting...",
            [LobbyPlayerStatus.Connected] = "Connected",
            [LobbyPlayerStatus.Readied] = "READY",
        };

        [OnReadyGet]
        private Label nameLabel;
        [OnReadyGet]
        private Label statusLabel;
        [OnReadyGet]
        private Label scoreLabel;

        public Player Player { get; private set; }

        private LobbyPlayerStatus status;
        public LobbyPlayerStatus Status
        {
            get => status;
            set
            {
                status = value;
                statusLabel.Text = LobbyPlayerStatusMessages[value];
            }
        }
        private int score;
        public int Score
        {
            get => score;
            set
            {
                score = value;
                if (score == 0)
                    scoreLabel.Text = "";
                else
                    scoreLabel.Text = value.ToString();
            }
        }

        public void Construct(Player player, LobbyPlayerStatus status = LobbyPlayerStatus.Connecting, int score = 0)
        {
            nameLabel.Text = player.Username;
            Status = status;
            Score = score;
        }
    }
}
