using Godot;
using System;
using Nakama;

public class Online : Node
{
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

    public delegate void SessionChangedDelegate(ISession session);
    public delegate void SessionConnectedDelegate(ISession session);
    public delegate void SocketConnectedDelegate(ISocket socket);

    public event SessionChangedDelegate SessionChanged;
    public event SessionConnectedDelegate SessionConnected;
    public event SocketConnectedDelegate SocketConnected;

    public ISession NakamaSession { get; private set; }
    public ISocket NakamaSocket { get; private set; }

    private bool nakamaSocketConnecting = false;

    public bool IsNakamaSocketConnected => NakamaSocket != null && NakamaSocket.IsConnected;

    public void SetNakamaSession(ISession session)
    {
        NakamaSession = session;

        SessionChanged?.Invoke(session);

        if (NakamaSession != null)
            SessionConnected?.Invoke(session);
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
