using Godot;
using System;
using System.Collections.Generic;
using NakamaWebRTC;
using GDC = Godot.Collections;
using Fractural.GodotCodeGenerator.Attributes;
using System.Linq;

namespace NakamaWebRTCDemo
{
    public partial class Game : Node2D
    {
        [Export]
        private PackedScene playerPrefab;
        [Export]
        private PackedScene mapPrefab;

        private Node2D map;
        [OnReadyGet]
        private Node2D playerContainer;

        [Export]
        public bool HasGameStarted { get; set; } = false;
        [Export]
        public bool IsGameOver { get; set; } = false;

        public List<GamePlayer> GamePlayers { get; private set; } = new List<GamePlayer>();

        public event Action OnGameStarted;
        public event Action<int> PlayerDied;
        public event Action<int> OnGameOver;

        // Should only be ran by the server
        public void LoadAndStartGame(List<Player> players)
        {
            if (GameState.Global.OnlinePlay)
                Rpc(nameof(SetupGame), players);
            else
                SetupGame(players);
        }

        public GamePlayer GetGamePlayer(Player player) => GetGamePlayer(player.PeerID);
        public GamePlayer GetGamePlayer(int playerID)
        {
            return GamePlayers.Find(x => x.Player.PeerID == playerID);
        }

        // Used primarily by the GameSession to remove players
        // that have disconnected.
        public void RemovePlayer(Player player)
        {
            var gamePlayerIndex = GamePlayers.FindIndex(x => x.Player == player);
            if (gamePlayerIndex >= 0)
            {
                GamePlayers[gamePlayerIndex].Kill();
            }
        }

        [RemoteSync]
        private void SetupGame(List<Player> players)
        {
            GetTree().Paused = true;

            if (HasGameStarted)
                StopGame();

            IsGameOver = false;
            HasGameStarted = true;

            // Reset Map
            map = mapPrefab.Instance<Node2D>();
            AddChild(map);

            // Respawn players
            foreach (var player in players)
            {
                var gamePlayerInst = playerPrefab.Instance<GamePlayer>();
                playerContainer.AddChild(gamePlayerInst);
                gamePlayerInst.Construct(player);
                gamePlayerInst.GlobalPosition = map.GetNode<Node2D>("PlayerStartPositions/Player" + player.PeerID).GlobalPosition;
                gamePlayerInst.Death += () => OnPlayerDeath(player.PeerID);
                GamePlayers.Add(gamePlayerInst);

                if (GameState.Global.OnlinePlay)
                    gamePlayerInst.Input.Mode = PlayerInput.ModeEnum.Synchronize;
                else
                {
                    gamePlayerInst.Input.Mode = PlayerInput.ModeEnum.Control;
                    gamePlayerInst.Input.InputPrefix = $"player{player.PeerID}_";
                }
            }

            if (GameState.Global.OnlinePlay)
            {
                var myGamePlayer = GetGamePlayer(GetTree().GetNetworkUniqueId());
                myGamePlayer.Input.Mode = PlayerInput.ModeEnum.Control;
                myGamePlayer.Input.InputPrefix = $"player1_";
                // Tell server we've succesfully setup the game
                RpcId(1, nameof(FinishedGameSetup), GetTree().GetNetworkUniqueId());
            }
            else
            {
                StartGame();
            }
        }

        // Records when each player has finsihed setup
        // MasterSync only accepts calls from puppets and the master peer itself.
        // In this case, the server (peer 1) is receiving
        // "readied" calls from the others
        [MasterSync]
        public void FinishedGameSetup(int playerID)
        {
            // Once all clients are set up tell them to start the game
            // We do this waiting in case the map load slower on
            // one person's machine.
            var gamePlayer = GetGamePlayer(playerID);
            gamePlayer.IsSetUp = true;
            if (GamePlayers.All(x => x.IsSetUp))
                Rpc(nameof(StartGame));
        }

        // Actually start the game
        // RemoteSync calls the method on the caller and all other peers.
        [RemoteSync]
        public void StartGame()
        {
            HasGameStarted = true;
            GetTree().Paused = false;
        }

        public void StopGame()
        {
            HasGameStarted = false;
            if (map != null)
                map.QueueFree();
            foreach (Node child in playerContainer.GetChildren())
                child.QueueFree();
            GamePlayers.Clear();
        }

        private void OnPlayerDeath(int playerID)
        {
            PlayerDied?.Invoke(playerID);

            GamePlayers.RemoveAll(x => x.Player.PeerID == playerID);

            // If there's only one player alive
            if (GamePlayers.Count() == 1)
            {
                IsGameOver = true;
                OnGameOver?.Invoke(GamePlayers[0].Player.PeerID);
            }
        }
    }
}
