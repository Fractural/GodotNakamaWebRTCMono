using Fractural.GodotCodeGenerator.Attributes;
using Godot;

namespace NakamaWebRTCDemo
{
    public partial class GamePlayerDeathFX : RigidBody2D
    {
        [OnReadyGet]
        private GamePlayer gamePlayer;
        [OnReadyGet]
        private Sprite playerSprite;

        [Export]
        private Vector2 forceRange = new Vector2(1000, 2000);

        [OnReady]
        public void RealReady()
        {
            gamePlayer.Death += OnDeath;
        }

        private void OnDeath()
        {
            Color color = playerSprite.Modulate;
            color.a = 0.5f;
            playerSprite.Modulate = color;
            playerSprite.GetParent().RemoveChild(playerSprite);
            playerSprite.Position = Vector2.Zero;
            AddChild(playerSprite);
            var originalTransform = GetGlobalTransform();
            var playerParent = GetParent().GetParent();
            GetParent().RemoveChild(this);
            playerParent.AddChild(this);
            GlobalTransform = originalTransform;
            ApplyImpulse(Vector2.Zero, new Vector2(GD.Randf(), GD.Randf()).Normalized() * (float)GD.RandRange(forceRange.x, forceRange.y));
        }

        public override void _IntegrateForces(Physics2DDirectBodyState state)
        {
            RotationDegrees = 0;
        }
    }
}
