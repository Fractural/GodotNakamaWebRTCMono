using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using NakamaWebRTC;
using System;

namespace NakamaWebRTCDemo
{
    public partial class GamePlayer : KinematicBody2D
    {
        // Player stores everything controlleed
        // by an input node here. In this basic example it's
        // moving and attacking
        [OnReadyGet]
        public IMovement Movement { get; private set; }
        [OnReadyGet]
        public IAttack Attack { get; private set; }
        [OnReadyGet]
        public PlayerInput Input { get; private set; }

        [OnReadyGet]
        private Label usernameLabel;

        public Player Player { get; set; }
        public bool IsSetUp { get; set; } = false;
        public bool IsDead { get; set; } = false;

        public event Action Death;

        public void Construct(Player player)
        {
            Player = player;
            // Note that SetNetworkMaster is recursive by default
            SetNetworkMaster(player.PeerID);
            usernameLabel.Text = player.Username;
        }

        public void Kill()
        {
            IsDead = true;
            Death?.Invoke();
        }
    }
}
