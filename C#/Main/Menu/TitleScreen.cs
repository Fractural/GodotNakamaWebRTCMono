using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using System;

namespace NakamaWebRTCDemo
{
    public partial class TitleScreen : Screen
    {
        [OnReadyGet]
        private LocalGame localGame;
        [OnReadyGet]
        private Button localButton;
        [OnReadyGet]
        private Button onlineButton;

        [OnReady]
        public void RealReady()
        {
            localButton.Connect("pressed", this, nameof(OnLocalButtonPressed));
            onlineButton.Connect("pressed", this, nameof(OnOnlineButtonPressed));
        }

        private void OnOnlineButtonPressed() => uiLayer.ShowScreen(nameof(ConnectionScreen));

        private void OnLocalButtonPressed() => localGame.StartGame();
    }
}
