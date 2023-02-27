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
        public Action RoundOverActionOverride { get; set; }

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

        // Ran on everyone
        //
        // initializedPlayers is only set the first time the session is started.
        // Afterwords, we maintain the GameSessionPlayers list using
        // RemovePlayer calls made from OnlineGame.
        public void StartSession(List<Player> intializedPlayers = null)
        {
            if (intializedPlayers != null)
                GameSessionPlayers = intializedPlayers.Select(x => new GameSessionPlayer()
                {
                    Player = x
                }).ToList();
            StartGame();
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

        public void RemovePlayer(Player player)
        {
            game.RemovePlayer(player);
            GameSessionPlayers.RemoveAll(x => x.Player == player);
        }

        // Called on everyone
        private void StartGame()
        {
            // Inject the players every time we start the game
            // This is incase a player leaves in the middle of the match,
            // we can still continue with the remaining 
            game.LoadAndStartGame(GameSessionPlayers.Select(x => x.Player).ToList());
        }

        private async void StopSession()
        {
            game.StopGame();
            if (GameState.Global.OnlinePlay)
            {
                await OnlineMatch.Global.Leave();
                uiLayer.ShowScreen(nameof(MatchScreen));
            }
            else
                uiLayer.ShowScreen(nameof(TitleScreen));
        }

        private void RestartGame()
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
            if (!GameState.Global.OnlinePlay)
            {
                // Local games only have rounds and no matches
                ShowResults(winningPlayerID);
            }
            else if (GetTree().IsNetworkServer())
            {
                var winningSessionPlayer = GetSessionPlayer(winningPlayerID);
                winningSessionPlayer.Score += 1;
                bool isMatchOver = winningSessionPlayer.Score >= WinningScore;
                Rpc(nameof(ShowResults), winningPlayerID, winningSessionPlayer.Score, isMatchOver);
            }
        }

        [RemoteSync]
        private async void ShowResults(int playerID = 0, int score = 0, bool isMatchOver = false)
        {
            var winningSessionPlayer = GetSessionPlayer(playerID);
            if (isMatchOver)
                uiLayer.ShowMessage(winningSessionPlayer.Player.Username + " wins the whole match!", 4f);
            else
                uiLayer.ShowMessage(winningSessionPlayer.Player.Username + " wins this round!", 4f);

            await ToSignal(GetTree().CreateTimer(2.0f), "timeout");

            if (GameState.Global.OnlinePlay)
            {
                if (RoundOverActionOverride != null)
                {
                    RoundOverActionOverride();
                    RoundOverActionOverride = null;
                } else if (isMatchOver)
                    StopSession();
                else
                {
                    // We must synchronize our winning session player to that of the server's
                    if (!GetTree().IsNetworkServer())
                        winningSessionPlayer.Score = score;

                    uiLayer.ShowScreen(nameof(LobbyScreen), new LobbyScreen.Args()
                    {
                        GameSessionPlayers = GameSessionPlayers
                    });
                }
            }
            else
            {
                RestartGame();
            }
        }
    }
}
