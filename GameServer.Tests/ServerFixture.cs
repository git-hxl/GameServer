using System.Threading.Channels;
using LiteNetLib;
using Serilog;
using SharedLib.Config;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;

namespace GameServer.Tests;

public sealed class ServerFixture : IDisposable
{
    public int LobbyClientPort { get; }
    public int LobbyServerPort { get; }
    public int GameServerPort { get; }
    public const string ClientKey = "Client@TestKey";
    public const string ServerKey = "Server@TestKey";

    private LobbyServer.LobbyServer _lobby = null!;
    private global::GameServer.GameServer _game = null!;
    private CancellationTokenSource _cts = new();
    private Task _pollTask = null!;

    public ServerFixture()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

        LobbyClientPort = TestPorts.NextFree();
        LobbyServerPort = TestPorts.NextFree();
        GameServerPort = TestPorts.NextFree();

        var lobbyConfig = new LobbyServerConfig
        {
            ClientPort = LobbyClientPort,
            ServerPort = LobbyServerPort,
            ClientConnectionKey = ClientKey,
            ServerConnectionKey = ServerKey
        };
        _lobby = new LobbyServer.LobbyServer(lobbyConfig);
        _lobby.Start(lobbyConfig);

        var gameConfig = new GameServerConfig
        {
            Port = GameServerPort,
            LobbyAddress = "127.0.0.1",
            LobbyPort = LobbyServerPort,
            LobbyConnectionKey = ServerKey,
            ClientConnectionKey = ClientKey
        };
        _game = new global::GameServer.GameServer(gameConfig);
        _game.Start();

        _pollTask = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(5));
            while (!_cts.Token.IsCancellationRequested)
            {
                _lobby.PollEvents();
                _game.PollEvents();
                try
                {
                    await timer.WaitForNextTickAsync(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        });

        Thread.Sleep(1000);
    }

    public void StopGameServer()
    {
        _game.Stop();
        Thread.Sleep(300);
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _pollTask.Wait(2000);
        }
        catch (AggregateException)
        {
        }
    }
}

public sealed class TestClient : IDisposable
{
    private readonly NetManager _client;
    private readonly EventBasedNetListener _listener;
    public NetPeer? Peer { get; private set; }
    public bool Connected => Peer?.ConnectionState == ConnectionState.Connected;
    private readonly object _lock = new();
    private readonly List<(ushort MessageId, ReturnCode Code, byte[] Payload)> _messages = [];

    public TestClient()
    {
        _listener = new EventBasedNetListener();
        _listener.PeerConnectedEvent += peer => Peer = peer;
        _listener.NetworkReceiveEvent += (_, reader, _, _) =>
        {
            try
            {
                var frame = MessageHelper.ReadFrame(reader);
                lock (_lock)
                {
                    _messages.Add((frame.MessageId, frame.Code, frame.Payload));
                }
            }
            finally
            {
                reader.Recycle();
            }
        };
        _client = new NetManager(_listener);
        _client.Start();
    }
    public void Connect(int port, string key)
    {
        _client.Connect("127.0.0.1", port, key);
    }

    public bool WaitConnected(int timeoutMs = 5000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            Poll();
            if (Connected) return true;
            Thread.Sleep(5);
        }
        return false;
    }

    public void Poll() => _client.PollEvents();

    public void Send(ushort messageId, object data)
    {
        if (Peer == null) return;
        MessageHelper.Send(Peer, messageId, ReturnCode.Success, data);
    }

    public (ushort MessageId, ReturnCode Code, byte[] Payload)? WaitMessage(ushort messageId, int timeoutMs = 5000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            Poll();
            lock (_lock)
            {
                var msg = _messages.Find(m => m.MessageId == messageId);
                if (msg != default)
                    return msg;
            }
            Thread.Sleep(5);
        }
        return null;
    }

    public T? WaitPayload<T>(ushort messageId, int timeoutMs = 5000) where T : class
    {
        var msg = WaitMessage(messageId, timeoutMs);
        return msg is { } m ? MessageHelper.Deserialize<T>(m.Payload) : null;
    }    public void Dispose()
    {
        _client.Stop();
    }
}
