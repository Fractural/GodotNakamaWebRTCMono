using Fractural.GodotCodeGenerator.Attributes;
using Godot;

namespace NakamaWebRTCDemo
{
    public partial class KinematicBodyMovement : Node, IMovement, IEnable
    {
        [OnReadyGet]
        private KinematicBody2D body;

        [Export]
        public bool Enabled { get; set; } = true;
        [Export]
        public Vector2 Direction { get; set; }
        [Export]
        public float Speed { get; set; } = 10f;

        public override void _PhysicsProcess(float delta)
        {
            if (!Enabled || this.TryIsNotNetworkMaster()) return;

            body.MoveAndSlide(Direction * Speed * delta);

            this.TryRpc(RpcType.Local | RpcType.Master | RpcType.Unreliable, nameof(UpdateRemoteBody), body.GlobalPosition);
        }

        [Puppet]
        private void UpdateRemoteBody(Vector2 position)
        {
            body.GlobalPosition = position;
        }
    }
}
