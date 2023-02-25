using Godot;
using System;
using System.Collections.Generic;
using NakamaWebRTC;
using GDC = Godot.Collections;

namespace NakamaWebRTCDemo
{
    public class GamePlayer
    {
        public Player Player { get; set; }
        public bool IsSetUp { get; set; } = false;
        public bool Dead { get; set; } = false;

        public static GamePlayer FromPlayer(Player player)
        {
            return new GamePlayer()
            {
                Player = player
            };
        }
    }

    public class Game : Node2D
    {
        [Export]
        private PackedScene playerPrefab;
        private PackedScene mapScene;

        private Node2D map;
        private Node2D players_node;

        [Export]
        public bool GameStarted { get; set; } = false;
        [Export]
        public bool GameOver { get; set; } = false;

        public IReadOnlyCollection<GamePlayer> GamePlayers => PeerIDToGamePlayers.Values;
        public Dictionary<string, GamePlayer> PeerIDToGamePlayers { get; set; } = new Dictionary<string, GamePlayer>();

        public event Action OnGameStarted;
        public event Action<string> OnPlayerDead;
        public event Action<string> OnGameOver;

        public void GameStart(GDC.Array<Player> players)
        {
            if (GameState.Global.OnlinePlay)
                Rpc(nameof(ResetGame), players);
            else
                ResetGame(players);
        }

        [RemoteSync]
        public void ResetGame(GDC.Array<Player> players)
        {
            GetTree().Paused = true;

            if (GameStarted)
                StopGame();

            PeerIDToGamePlayers.Clear();
            //foreach (var player in players)
            //    PeerIDToGamePlayers

            //GameStarted = false;

            //int playerIndex = 1;
            //foreach (var player in )
        }

        [MasterSync]
        public void FinishedGameSetup(int playerID)
        {

        }

        // Actually start the game
        [RemoteSync]
        public void GameStart()
        {
            GameStarted = true;
        }

        public void StopGame()
        {
            throw new NotImplementedException();
        }
    }
}
