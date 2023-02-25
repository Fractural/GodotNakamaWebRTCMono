using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using NakamaWebRTC;
using System.Collections.Generic;

namespace NakamaWebRTCDemo
{
    public partial class Main : Node2D
    {
        [OnReadyGet]
        public Game Game { get; set; }
        [OnReadyGet]
        public UILayer UILayer { get; set; }
        [OnReadyGet]
        public TitleScreen TitleScreen { get; set; }

        public Dictionary<string, Player> Players = new Dictionary<string, Player>();

        [OnReady]
        public void RealReady()
        {
            OnlineMatch.Global.OnError += OnOnlineMatchError;
            OnlineMatch.Global.Disconnected += OnOnlineMatchDisconnected;
            OnlineMatch.Global.PlayerStatusChanged += OnOnlineMatchPlayerStatusChanged;
            OnlineMatch.Global.PlayerLeft += OnOnlineMatchPlayerLeft;

            TitleScreen.LocalPlaySelected += OnPlayLocal;
            TitleScreen.OnlinePlaySelected += OnPlayOnline;
        }

        private void OnPlayLocal()
        {
            GameState.Global.OnlinePlay = false;
            UILayer.HideScreen();
        }

        private void OnPlayOnline()
        {
            GameState.Global.OnlinePlay = true;
            UILayer.ShowScreen("ConnectionScreen");
        }

        private void OnOnlineMatchPlayerLeft(Player obj)
        {
            throw new System.NotImplementedException();
        }

        private void OnOnlineMatchPlayerStatusChanged(Player arg1, PlayerStatus arg2)
        {
            throw new System.NotImplementedException();
        }

        private void OnOnlineMatchDisconnected()
        {
            throw new System.NotImplementedException();
        }

        private void OnOnlineMatchError(string obj)
        {
            throw new System.NotImplementedException();
        }
    }
}
