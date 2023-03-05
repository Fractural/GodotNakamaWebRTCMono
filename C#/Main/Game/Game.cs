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
        public bool IsGameOver { get; set; } = false;

        public List<GamePlayer> GamePlayers { get; private set; } = new List<GamePlayer>();

        public event Action OnGameStarted;
        public event Action<int> PlayerDied;
        public event Action<int> OnGameOver;

        // Ran on everyone
        public void LoadAndStartGame(List<Player> players)
        {
            StartSetup(players);
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

        private void StartSetup(List<Player> players)
        {
            StopGame();

            IsGameOver = false;

            // Reset Map
            map = mapPrefab.Instance<Node2D>();
            AddChild(map);

            players.Sort((p1, p2) => p1.PeerID - p2.PeerID);
            Console.Print("Players: " + players + "UniqueID: " + GetTree().GetNetworkUniqueId());

            // Respawn players
            // Note that we need a playerIdx counter to assign spawn positions, since we cannot rely on
            // PeerID to be < total # of players. If people keep joining/leaving a match, this could
            // bump the PeerID over the number of actual players left in a match.
            int playerIdx = 1;
            foreach (var player in players)
            {
                var gamePlayerInst = playerPrefab.Instance<GamePlayer>();
                playerContainer.AddChild(gamePlayerInst);
                gamePlayerInst.Construct(player);
                gamePlayerInst.GlobalPosition = map.GetNode<Node2D>("PlayerStartPositions/Player" + playerIdx).GlobalPosition;
                gamePlayerInst.Death += () => OnPlayerDeath(player.PeerID);
                GamePlayers.Add(gamePlayerInst);

                if (GameState.Global.OnlinePlay)
                    gamePlayerInst.Input.Mode = PlayerInput.ModeEnum.Synchronize;
                else
                {
                    gamePlayerInst.Input.Mode = PlayerInput.ModeEnum.Control;
                    gamePlayerInst.Input.InputPrefix = $"player{playerIdx}_";
                }
                playerIdx++;
            }

            if (GameState.Global.OnlinePlay)
            {
                var myGamePlayer = GetGamePlayer(GetTree().GetNetworkUniqueId());
                Console.Print("Setting manual controls for player " + myGamePlayer.Player.PeerID);
                myGamePlayer.Input.Mode = PlayerInput.ModeEnum.Control;
                myGamePlayer.Input.InputPrefix = $"player1_";
                // Tell server we've succesfully setup the game
                RpcId(1, nameof(FinishedSetup), GetTree().GetNetworkUniqueId());
            }
            else
            {
                StartGame();
            }

            Console.Print("Start game with : {");
            foreach (Node child in playerContainer.GetChildren())
                Console.Print("  --> " + child.Name);
            Console.Print("}");
            
            return;
        }

        // Records when each player has finsihed setup
        // MasterSync only accepts calls from puppets and the master peer itself.
        // In this case, the server (peer 1) is receiving
        // "readied" calls from the others
        [MasterSync]
        private void FinishedSetup(int playerID)
        {
            // Once all clients are set up tell them to start the game
            // We do this waiting in case the map load slower on
            // one person's machine.
            var gamePlayer = GetGamePlayer(playerID);
            gamePlayer.IsSetUp = true;
            if (GamePlayers.All(x => x.IsSetUp))
            {
                Console.Print("All players are ready, calling start game");
                Rpc(nameof(StartGame));
            }
        }

        // Actually start the game
        // RemoteSync calls the method on the caller and all other peers.
        [RemoteSync]
        private void StartGame()
        {
            OnGameStarted?.Invoke();
            GetTree().Paused = false;
        }

        public void StopGame()
        {
            if (map != null)
                map.QueueFree();
            map = null;
            foreach (Node child in playerContainer.GetChildren())
            {
                playerContainer.RemoveChild(child);
                child.QueueFree();
            }
            GamePlayers.Clear();
        }

        private void OnPlayerDeath(int playerID)
        {
            PlayerDied?.Invoke(playerID);

            GamePlayers.RemoveAll(x => x.Player.PeerID == playerID);

            // If there's only one player alive
            if (GamePlayers.Count() == 1)
            {
                // Note that all players independently see a game over, but it's
                // ultimately the host who will show the winner to everyone.
                IsGameOver = true;
                OnGameOver?.Invoke(GamePlayers[0].Player.PeerID);
            }
        }
    }
}
