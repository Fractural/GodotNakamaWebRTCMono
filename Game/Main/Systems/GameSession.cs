using Fractural.GodotCodeGenerator.Attributes;
using Fractural.Utils;
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

        public override string ToString() => $"{{Player: {Player} Score: {Score}}}";
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
    public partial class GameSession : Node, IBufferSerializable
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

        public GameSessionPlayer AddPlayer(Player player)
        {
            if (!GameSessionPlayers.Any(x => x.Player == player))
            {
                var newSessionPlayer = new GameSessionPlayer()
                {
                    Player = player
                };
                GameSessionPlayers.Add(newSessionPlayer);
                return newSessionPlayer;
            }
            return null;
        }

        public void RemovePlayer(Player player)
        {
            if (GameSessionPlayers.Any(x => x.Player == player))
            {
                game.RemovePlayer(player);
                GameSessionPlayers.RemoveAll(x => x.Player == player);
            }
        }

        // Ran on everyone
        public void StartGame()
        {
            Console.Print($"Start game with {string.Join(",", GameSessionPlayers)}");
            // Inject the players every time we start the game
            // This is incase a player leaves in the middle of the match,
            // we can still continue with the remaining 
            game.LoadAndStartGame(GameSessionPlayers.Select(x => x.Player).ToList());
        }

        public void RestartGame() => StartGame();

        public void StopSession()
        {
            Reset();
            SessionStopped?.Invoke();
        }

        public void Serialize(StreamPeerBuffer buffer)
        {
            buffer.Put32(GameSessionPlayers.Count);
            foreach (var player in GameSessionPlayers)
            {
                buffer.Put32(player.Player.PeerID);
                buffer.Put32(player.Score);
            }
        }

        public void Deserialize(StreamPeerBuffer buffer)
        {
            int count = buffer.Get32();
            Console.Print("GameSession Deserialize: {");
            for (int i = 0; i < count; i++)
            {
                int peerID = buffer.Get32();
                int score = buffer.Get32();

                GetSessionPlayer(peerID).Score = score;
                Console.Print($"  --> [{peerID}] = {score}");
            }
            Console.Print("}");
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
            Console.Print("Player died: " + peerID);
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
            this.TryRpc(RpcType.Local | RpcType.Server, nameof(ShowResults), winningPlayerID, winningSessionPlayer.Score, isMatchOver);
        }

        // Called on everyone
        [RemoteSync]
        private async void ShowResults(int playerID = 0, int score = 0, bool isMatchOver = false)
        {
            Console.Print($"Winning player score: [{playerID}] = {score}");
            var winningSessionPlayer = GetSessionPlayer(playerID);

            Console.Print($"Show results with {string.Join(",", GameSessionPlayers)}");

            if (isMatchOver)
                uiLayer.ShowMessage(winningSessionPlayer.Player.Username + " wins the whole match!", 4f);
            else
                uiLayer.ShowMessage(winningSessionPlayer.Player.Username + " wins this round!", 4f);

            await ToSignal(GetTree().CreateTimer(2.0f), "timeout");

            RoundFinished?.Invoke(isMatchOver);
        }
    }
}
