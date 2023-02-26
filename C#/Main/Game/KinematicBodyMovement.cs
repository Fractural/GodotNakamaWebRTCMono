using Fractural.GodotCodeGenerator.Attributes;
using Godot;

namespace NakamaWebRTCDemo
{
    public partial class KinematicBodyMovement : Node, IMovement
    {
        [OnReadyGet]
        private KinematicBody2D body;

        public Vector2 Direction { get; set; }
        public float Speed { get; set; }

        public override void _PhysicsProcess(float delta)
        {
            body.MoveAndSlide(Direction * Speed * delta);

            if (GameState.Global.OnlinePlay)
                Rpc(nameof(UpdateRemoteBody), body.GlobalPosition);
        }

        [Puppet]
        private void UpdateRemoteBody(Vector2 position)
        {
            body.GlobalPosition = position;
        }
    }
}
