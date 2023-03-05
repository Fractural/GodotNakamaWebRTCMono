using Godot;
using System;
using Nakama;
using System.Threading.Tasks;
using Fractural.GodotCodeGenerator.Attributes;

namespace NakamaWebRTCDemo
{
    public partial class Online : Node
    {
        public static Online Global { get; private set; }

        [Export]
        public string NakamaServerKey = "defaultkey";
        [Export]
        public string NakamaHost = "localhost";
        [Export]
        public int NakamaPort = 7350;
        [Export]
        public string NakamaScheme = "http";

        private Client nakamaClient;

        [Awaitable]
        public event Action<ISession> SessionChanged;
        [Awaitable]
        public event Action<ISession> SessionConnected;
        [Awaitable]
        public event Action<ISocket> SocketConnected;
        public event Action<Exception> NakamaConnectionError;

        private ISession nakamaSession;
        public ISession NakamaSession
        {
            get => nakamaSession; set
            {
                nakamaSession = value;

                SessionChanged?.Invoke(value);

                if (nakamaSession != null)
                    SessionConnected?.Invoke(value);
            }
        }
        public ISocket NakamaSocket { get; private set; }

        private bool nakamaSocketConnecting = false;
        private GodotHttpAdapter godotHttpAdapter;
        private GodotWebSocketAdapter godotWebSocketAdapter;

        public bool IsNakamaSocketConnected => NakamaSocket != null && NakamaSocket.IsConnected;

        public override void _Ready()
        {
            if (Global != null)
            {
                QueueFree();
                return;
            }
            Global = this;

            godotHttpAdapter = new GodotHttpAdapter();
            godotWebSocketAdapter = new GodotWebSocketAdapter();
            AddChild(godotHttpAdapter);
            AddChild(godotWebSocketAdapter);
            nakamaClient = new Client(
                scheme: NakamaScheme,
                host: NakamaHost,
                port: NakamaPort,
                serverKey: NakamaServerKey,
                adapter: godotHttpAdapter
            );
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                if (Global == this)
                    Global = null;
            }
        }

        public async void ConnectNakamaSocket()
        {
            await CallNakama(async (client) =>
            {
                if (NakamaSocket != null) return;
                if (nakamaSocketConnecting) return;
                nakamaSocketConnecting = true;

                NakamaSocket = Socket.From(client, godotWebSocketAdapter);
                await NakamaSocket.ConnectAsync(NakamaSession);
                nakamaSocketConnecting = false;
                SocketConnected?.Invoke(NakamaSocket);
            });
        }

        /// <summary>
        /// Use this for all your nakama calls.
        /// This handles the case when the whole nakama server is down, where we won't event get
        /// an ApiResponseException back. When this happens, NakamConnectionError is invoked.
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <returns></returns>
        public async Task CallNakama(Func<Client, Task> asyncFunc)
        {
            try
            {
                await asyncFunc(nakamaClient);
            }
            catch (Exception e) when (!(e is ApiResponseException))
            {
                GD.Print($"{nameof(Online)}: Encountered Exception: {e}");
                // We catch any exception that is not an APIResponseException
                // APIResponseException is normal behaviour
                NakamaConnectionError.Invoke(e);
            }
        }
    }
}