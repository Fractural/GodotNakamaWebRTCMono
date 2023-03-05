using Godot;
using System;
using Fractural.GodotCodeGenerator.Attributes;

namespace NakamaWebRTCDemo
{
    public partial class OnlineErrorHandler : Node
    {
        [OnReadyGet]
        private UILayer uiLayer;
        [OnReadyGet]
        private Game game;

        [OnReady]
        public void RealReady()
        {
            Online.Global.NakamaConnectionError += NakamaConnectionError;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                if (Online.Global != null)
                    Online.Global.NakamaConnectionError -= NakamaConnectionError;
            }
        }

        private void NakamaConnectionError(Exception ex)
        {
            game.StopGame();
            uiLayer.ShowScreen(nameof(TitleScreen));
            uiLayer.ShowMessage("Cannot connect to Nakama server. Is Nakama running?", 2f);
        }
    }
}