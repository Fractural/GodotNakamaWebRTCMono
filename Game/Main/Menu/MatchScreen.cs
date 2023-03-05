using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using NakamaWebRTC;
using System;
using System.Collections.Generic;
using GDC = Godot.Collections;

namespace NakamaWebRTCDemo
{
    public partial class MatchScreen : Screen
    {
        [OnReadyGet]
        private IOnlineGame onlineGame;
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
        [OnReadyGet]
        private Button pasteButton;

        [OnReady]
        public void RealReady()
        {
            matchButton.Connect("pressed", this, nameof(OnMatchButtonPressed), Utils.GDParams(MatchMode.Matchmaker));
            createButton.Connect("pressed", this, nameof(OnMatchButtonPressed), Utils.GDParams(MatchMode.Create));
            joinButton.Connect("pressed", this, nameof(OnMatchButtonPressed), Utils.GDParams(MatchMode.Join));
            pasteButton.Connect("pressed", this, nameof(OnPasteButtonPressed));

            OnlineMatch.Global.MatchmakerMatched += OnMatchmakerMatched;
            OnlineMatch.Global.MatchCreated += OnMatchCreated;
            OnlineMatch.Global.MatchJoined += OnMatchJoined;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                if (OnlineMatch.Global != null)
                {
                    OnlineMatch.Global.MatchmakerMatched -= OnMatchmakerMatched;
                    OnlineMatch.Global.MatchCreated -= OnMatchCreated;
                    OnlineMatch.Global.MatchJoined -= OnMatchJoined;
                }
            }
        }

        public override void ShowScreen(object args)
        {
            base.ShowScreen(args);

            matchmakerPlayerCountSpinbox.Value = 2;
            joinMatchIDControl.Text = "";
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

                await Online.Global.SessionConnectedRaised();

                if (Online.Global.NakamaSession == null)
                    return;
            }

            // Connect socket to realtime Nakama APPI if not connected
            if (!Online.Global.IsNakamaSocketConnected)
            {
                Online.Global.ConnectNakamaSocket();
                await Online.Global.SocketConnectedRaised();
            }

            uiLayer.HideMessage();

            switch (mode)
            {
                case MatchMode.Matchmaker:
                    StartMatchmaking();
                    break;
                case MatchMode.Create:
                    CreateMatch();
                    break;
                case MatchMode.Join:
                    JoinMatch();
                    break;
            }
        }

        private void StartMatchmaking()
        {
            int minPlayers = (int)matchmakerPlayerCountSpinbox.Value;

            uiLayer.HideScreen();
            uiLayer.ShowMessage("Looking or a match...");

            OnlineMatch.Global.StartMatchmaking(Online.Global.NakamaSocket, new MatchmakingArgs()
            {
                MinCount = minPlayers,
                stringProperties = new Dictionary<string, string>()
                {
                    ["game"] = "test_game"
                },
                Query = "+properties.game:test_game"
            });
        }

        private void OnMatchmakerMatched(IReadOnlyCollection<Player> players)
        {
            uiLayer.HideMessage();
            onlineGame.MatchmakerMatched(players);
        }

        private void CreateMatch()
        {
            OnlineMatch.Global.CreateMatch(Online.Global.NakamaSocket);
        }

        private void OnMatchCreated(string matchID)
        {
            onlineGame.MatchCreated(matchID);
        }

        private void JoinMatch()
        {
            string matchID = joinMatchIDControl.Text.StripEdges();
            if (matchID == "")
            {
                uiLayer.ShowMessage("Need to paste Match ID to join", 2f);
                return;
            }
            if (!matchID.EndsWith("."))
                matchID += ".";

            OnlineMatch.Global.JoinMatch(Online.Global.NakamaSocket, matchID);
        }

        private void OnMatchJoined(string matchID)
        {
            onlineGame.MatchJoined(matchID);
        }

        private void OnPasteButtonPressed()
        {
            joinMatchIDControl.Text = OS.Clipboard;
        }
    }
}
