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
        public Client NakamaClient
        {
            get
            {
                if (nakamaClient == null)
                {
                    nakamaClient = new Client(
                        scheme: NakamaScheme,
                        host: NakamaHost,
                        port: NakamaPort,
                        serverKey: NakamaServerKey
                    );
                }
                return nakamaClient;
            }
        }

        [Awaitable]
        public event Action<ISession> SessionChanged;
        [Awaitable]
        public event Action<ISession> SessionConnected;
        [Awaitable]
        public event Action<ISocket> SocketConnected;

        private ISession nakamaSession;
        public ISession NakamaSession
        {
            get => nakamaSession; set
            {
                NakamaSession = value;

                SessionChanged?.Invoke(value);

                if (NakamaSession != null)
                    SessionConnected?.Invoke(value);
            }
        }
        public ISocket NakamaSocket { get; private set; }

        private bool nakamaSocketConnecting = false;

        public bool IsNakamaSocketConnected => NakamaSocket != null && NakamaSocket.IsConnected;

        public override void _Ready()
        {
            if (Global == null)
            {
                QueueFree();
                return;
            }
            Global = this;
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
            if (NakamaSocket != null) return;
            if (nakamaSocketConnecting) return;
            nakamaSocketConnecting = true;
            NakamaSocket = Socket.From(NakamaClient);
            await NakamaSocket.ConnectAsync(NakamaSession);
            nakamaSocketConnecting = false;
            SocketConnected?.Invoke(NakamaSocket);
        }
    }
}