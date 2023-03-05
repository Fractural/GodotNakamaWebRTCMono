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

        public GameSessionPlayer SessionPlayer { get; private set; }

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

        public void Construct(GameSessionPlayer player, LobbyPlayerStatus status = LobbyPlayerStatus.Connecting)
        {
            SessionPlayer = player;
            nameLabel.Text = $"{player.Player.Username} [{player.Player.PeerID}]";
            Status = status;

            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            scoreLabel.Text = SessionPlayer.Score > 0 ? SessionPlayer.Score.ToString() : "";
            Console.Print($"Updating lobby player [{SessionPlayer.Player.PeerID}] text = {scoreLabel.Text}");
        }
    }
}
