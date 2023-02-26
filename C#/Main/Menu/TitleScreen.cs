using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using System;

namespace NakamaWebRTCDemo
{
    public partial class TitleScreen : Screen
    {
        public event Action LocalPlaySelected;
        public event Action OnlinePlaySelected;

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

        private void OnOnlineButtonPressed() => OnlinePlaySelected?.Invoke();

        private void OnLocalButtonPressed() => LocalPlaySelected?.Invoke();
    }
}
