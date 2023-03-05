using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using NakamaWebRTC;
using System;

namespace NakamaWebRTCDemo
{
    public partial class GamePlayer : KinematicBody2D, IEnable
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
        private CollisionShape2D collider;
        [OnReadyGet]
        private Label usernameLabel;

        public Player Player { get; set; }

        [Export]
        private bool enabled = true;
        public bool Enabled
        {
            get => enabled;
            set
            {
                enabled = value;
                Movement.Enabled = value;
                Attack.Enabled = value;
                Input.Enabled = value;
                collider.Disabled = !value;
            }
        }
        // Used by host
        [Export]
        public bool IsSetUp { get; set; } = false;
        [Export]
        public bool IsDead { get; set; } = false;

        public event Action Death;

        public void Construct(Player player)
        {
            Player = player;
            // Note that SetNetworkMaster is recursive by default
            if (GameState.Global.OnlinePlay)
                SetNetworkMaster(player.PeerID);
            usernameLabel.Text = $"{player.Username} [{player.PeerID}]";
            Name = player.PeerID.ToString();
        }

        [RemoteSync]
        public void Kill()
        {
            if (IsDead || !Enabled)
                return;
            IsDead = true;
            Enabled = false;
            Death?.Invoke();
            Visible = false;
        }
    }
}
