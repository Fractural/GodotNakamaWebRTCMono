using Fractural.GodotCodeGenerator.Attributes;
using Godot;

namespace NakamaWebRTCDemo
{
    public partial class GamePlayerDeathFX : Node2D
    {
        [OnReadyGet]
        private GamePlayer gamePlayer;
        [OnReadyGet]
        private Sprite playerSprite;

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
        }
    }
}
