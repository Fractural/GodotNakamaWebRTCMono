using Fractural.GodotCodeGenerator.Attributes;
using Godot;

namespace NakamaWebRTCDemo
{
    public partial class LethalAttack : Area2D, IAttack
    {
        [OnReadyGet]
        private GamePlayer owner;
        [OnReadyGet]
        private AnimationPlayer animationPlayer;

        [Export]
        public float ChargeDuration { get; set; } = 0.5f;
        [Export]
        public float Cooldown { get; set; } = 2f;
        [Export]
        public bool CanUse { get; private set; } = true;

        public async void Use()
        {
            if (!CanUse)
                return;
            CanUse = false;

            if (GameState.Global.OnlinePlay)
                Rpc(nameof(ShowFx));
            else
                ShowFx();
            await ToSignal(GetTree().CreateTimer(ChargeDuration), "timeout");
            foreach (Node body in GetOverlappingBodies())
                if (body is GamePlayer player && player != owner && !player.IsDead)
                    player.Kill();
            await ToSignal(GetTree().CreateTimer(Cooldown), "timeout");
            CanUse = true;
        }

        [Puppet]
        private void ShowFx()
        {
            animationPlayer.Play("Attack");
        }
    }
}
