using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using NakamaWebRTC;
using GDC = Godot.Collections;

namespace NakamaWebRTCDemo
{
    public partial class MatchScreen : Screen
    {
        [OnReadyGet]
        private SpinBox matchmakerPlayerCountSpinbox;
        [OnReadyGet]
        private LineEdit joinMatchIDControl;
        [OnReadyGet]
        private Button matchButton;
        [OnReadyGet]
        private Button createButton;
        [OnReadyGet]
        private Button joinButton;

        [OnReady]
        public void RealReady()
        {
            matchButton.Connect("pressed", this, nameof(OnMatchButtonPressed), Utils.GDParams(MatchMode.Matchmaker));
            createButton.Connect("pressed", this, nameof(OnMatchButtonPressed), Utils.GDParams(MatchMode.Create));
            joinButton.Connect("pressed", this, nameof(OnMatchButtonPressed), Utils.GDParams(MatchMode.Join));

            OnlineMatch.Global.MatchmakerMatched += OnMatchmakerMatched;
            OnlineMatch.Global.MatchCreated += OnMatchCreated;
            OnlineMatch.Global.MatchJoined += OnMatchJoined;
        }

        private async void OnMatchButtonPressed(MatchMode mode)
        {
            // If our session has expired, show the connection screen
            if (Online.Global.NakamaSession == null || Online.Global.NakamaSession.IsExpired)
            {
                uiLayer.ShowScreen("ConnectionScreen", new
                {
                    nextScreen = "MatchScreen",
                    reconnect = true,
                }.ToGDDict());


                //await Online.Global.SessionChanged_Raised();

                if (Online.Global.NakamaSession == null)
                    return;
            }

            // TODO NOW: FInish this
        }

        public override void ShowScreen(GDC.Dictionary args)
        {
            base.ShowScreen(args);

            matchmakerPlayerCountSpinbox.Value = 2;
            joinMatchIDControl.Text = "";
        }

        private void OnMatchJoined(string obj)
        {
            throw new System.NotImplementedException();
        }

        private void OnMatchCreated(string obj)
        {
            throw new System.NotImplementedException();
        }

        private void OnMatchmakerMatched(System.Collections.Generic.IReadOnlyCollection<Player> obj)
        {
            throw new System.NotImplementedException();
        }
    }
}
