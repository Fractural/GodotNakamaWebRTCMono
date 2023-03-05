﻿using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using NakamaWebRTC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NakamaWebRTCDemo
{
    public class GameSessionPlayer
    {
        public Player Player { get; set; }
        public int Score { get; set; }
        // Used by host
        public bool IsSetUp { get; set; } = false;
    }

    /// <summary>
    /// Wraps around Game to handle the 
    /// lifecycle of rounds, and multiple matches.
    /// 
    /// Local games have infinite rounds, but no score keeping.
    /// 
    /// Multiplayer games are matched-based, and end when one player
    /// reaches a certain score. After the match is over the lobby is closed.
    /// </summary>
    public partial class GameSession : Node
    {
        public readonly int WinningScore = 5;

        [OnReadyGet]
        private UILayer uiLayer;
        [OnReadyGet]
        private Game game;

        public List<GameSessionPlayer> GameSessionPlayers { get; private set; } = new List<GameSessionPlayer>();
        // bool isMatchOver
        public event Action<bool> RoundFinished;
        public event Action SessionStopped;

        [OnReady]
        public void RealReady()
        {
            game.OnGameStarted += OnGameStarted;
            game.OnGameOver += OnGameOver;
            game.PlayerDied += OnPlayerDead;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                if (game != null)
                {
                    game.OnGameStarted -= OnGameStarted;
                    game.OnGameOver -= OnGameOver;
                    game.PlayerDied -= OnPlayerDead;
                }
            }
        }

        public void Reset()
        {
            GameSessionPlayers.Clear();
            game.StopGame();
        }

        public GameSessionPlayer GetSessionPlayer(int playerID)
        {
            return GameSessionPlayers.Find(x => x.Player.PeerID == playerID);
        }

        public void AddPlayers(IEnumerable<Player> players)
        {
            foreach (var player in players)
                AddPlayer(player);
        }

        public void AddPlayer(Player player)
        {
            GD.Print("Game Sesesion add player with peerID: " + player.PeerID);
            GameSessionPlayers.Add(new GameSessionPlayer()
            {
                Player = player
            });
        }

        public void RemovePlayer(Player player)
        {
            game.RemovePlayer(player);
            GameSessionPlayers.RemoveAll(x => x.Player == player);
        }

        // Ran on everyone
        public void StartGame()
        {
            GD.Print($"Start game with {string.Join(",", GameSessionPlayers)}");
            // Inject the players every time we start the game
            // This is incase a player leaves in the middle of the match,
            // we can still continue with the remaining 
            game.LoadAndStartGame(GameSessionPlayers.Select(x => x.Player).ToList());
        }

        public void StopSession()
        {
            Reset();
            SessionStopped?.Invoke();
        }

        public void RestartGame()
        {
            game.StopGame();
            StartGame();
        }

        // Called on everyone, since when the host makes the game start,
        // game.OnGameStarted will be invoked
        private void OnGameStarted()
        {
            uiLayer.HideAll();
            uiLayer.ShowBackButton();
            uiLayer.BackButtonActionOverride = StopSession;
        }

        private void OnPlayerDead(int peerID)
        {
            if (GameState.Global.OnlinePlay)
            {
                if (GetTree().GetNetworkUniqueId() == peerID)
                    uiLayer.ShowMessage("You died!");
            }
        }

        private void OnGameOver(int winningPlayerID)
        {
            var winningSessionPlayer = GetSessionPlayer(winningPlayerID);
            winningSessionPlayer.Score += 1;

            bool isMatchOver = winningSessionPlayer.Score >= WinningScore;
            this.TryRpc(nameof(ShowResults), winningPlayerID, winningSessionPlayer.Score, isMatchOver);
        }

        // Called on everyone
        [RemoteSync]
        private async void ShowResults(int playerID = 0, int score = 0, bool isMatchOver = false)
        {
            var winningSessionPlayer = GetSessionPlayer(playerID);
            winningSessionPlayer.Score = score;

            if (isMatchOver)
                uiLayer.ShowMessage(winningSessionPlayer.Player.Username + " wins the whole match!", 4f);
            else
                uiLayer.ShowMessage(winningSessionPlayer.Player.Username + " wins this round!", 4f);

            await ToSignal(GetTree().CreateTimer(2.0f), "timeout");

            RoundFinished?.Invoke(isMatchOver);
        }
    }
}
