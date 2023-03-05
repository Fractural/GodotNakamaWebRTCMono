using Godot;
using Fractural.GodotCodeGenerator.Attributes;
using NakamaWebRTC;

namespace NakamaWebRTCDemo
{
    public partial class OnlineMatchErrorHandler : Node
    {
        [OnReadyGet]
        private UILayer uiLayer;

        [OnReady]
        public void RealReady()
        {
            OnlineMatch.Global.OnError += OnError;
        }

        private void OnError(string message)
        {
            uiLayer.ShowScreen(nameof(MatchScreen));
            uiLayer.ShowMessage(message, 2);
        }
    }
}