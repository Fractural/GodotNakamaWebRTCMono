using Fractural.GodotCodeGenerator.Attributes;
using Godot;

namespace NakamaWebRTCDemo
{
    public partial class LethalAttack : Area2D, IAttack, IEnable
    {
        [OnReadyGet]
        private GamePlayer owner;
        [OnReadyGet]
        private AnimationPlayer animationPlayer;

        [Export]
        public bool Enabled { get; set; } = true;
        [Export]
        public float ChargeDuration { get; set; } = 0.5f;
        [Export]
        public float Cooldown { get; set; } = 2f;
        [Export]
        public bool CanUse { get; private set; } = true;

        public async void Use()
        {
            if (!CanUse || !Enabled || this.TryIsNotNetworkMaster())
                return;
            CanUse = false;

            this.TryRpc(RpcType.Local | RpcType.Master, nameof(ShowFx));
            await ToSignal(GetTree().CreateTimer(ChargeDuration), "timeout");
            foreach (Node body in GetOverlappingBodies())
                if (body is GamePlayer player && player != owner && !player.IsDead)
                    player.TryRpc(RpcType.Local, nameof(player.Kill));

            await ToSignal(GetTree().CreateTimer(Cooldown), "timeout");
            CanUse = true;
        }

        [PuppetSync]
        private void ShowFx()
        {
            animationPlayer.Play("Attack");
        }
    }
}
