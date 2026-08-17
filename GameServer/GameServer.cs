using LiteNetLib;
using LiteNetLib.Utils;
using Serilog;
using SharedLib.Config;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using GameServer.Handlers;
using GameServer.Player;
using GameServer.Room;

namespace GameServer;

public class GameServer
{
    private readonly NetManager _netManager;
    private readonly EventBasedNetListener _listener;

    private readonly NetManager _lobbyClient;
    private readonly EventBasedNetListener _lobbyListener;
    private volatile NetPeer? _lobbyPeer;

    private readonly string _clientKey;
    private readonly string _lobbyKey;
    private readonly string _lobbyAddress;
    private readonly int _lobbyPort;
    private readonly int _serverPort;

    private CancellationTokenSource _updateCts = new();
    private readonly PerformanceMonitor _perf = new();
    private GameRoomManager _roomManager = null!;
    private HandlerRegistry _gameRegistry = null!;
    private HandlerRegistry _lobbyRegistry = null!;

    public GameServer(GameServerConfig config)
    {
        _clientKey = config.ClientConnectionKey;
        _lobbyKey = config.LobbyConnectionKey;
        _lobbyAddress = config.LobbyAddress;
        _lobbyPort = config.LobbyPort;
        _serverPort = config.Port;

        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener)
        {
            UpdateTime = config.UpdateTime,
            PingInterval = config.PingInterval,
            DisconnectTimeout = config.DisconnectTimeout,
            ChannelsCount = config.ChannelsCount,
        };

        _lobbyListener = new EventBasedNetListener();
        _lobbyClient = new NetManager(_lobbyListener)
        {
            UpdateTime = config.UpdateTime,
            PingInterval = config.PingInterval,
            DisconnectTimeout = config.DisconnectTimeout,
            ChannelsCount = config.ChannelsCount
        };
    }

    public void Start()
    {
        _roomManager = new GameRoomManager(new GamePlayerManager());

        RegisterHandlers();

        _listener.ConnectionRequestEvent += OnConnectionRequest;
        _listener.PeerConnectedEvent += OnPeerConnected;
        _listener.PeerDisconnectedEvent += OnPeerDisconnected;
        _listener.NetworkReceiveEvent += OnNetworkReceive;
        _netManager.Start(_serverPort);

        _lobbyListener.PeerConnectedEvent += OnLobbyConnected;
        _lobbyListener.PeerDisconnectedEvent += OnLobbyDisconnected;
        _lobbyListener.NetworkReceiveEvent += OnLobbyReceive;
        _lobbyClient.Start();
        _lobbyClient.Connect(_lobbyAddress, _lobbyPort, _lobbyKey);

        _updateCts = new CancellationTokenSource();
        _ = UpdateLoop(_updateCts.Token);

        Log.Information("[GameServer] 启动 port={Port} lobbyAddress={Addr}:{LobbyPort}",
            _serverPort, _lobbyAddress, _lobbyPort);
    }

    private void RegisterHandlers()
    {
        _gameRegistry = new HandlerRegistry();
        _gameRegistry.Register(
            new JoinGameHandler(_roomManager),
            new LeaveGameHandler(_roomManager),
            new PositionSyncHandler(_roomManager),
            new AnimationSyncHandler(_roomManager),
            new ObjectSpawnHandler(_roomManager),
            new ObjectDespawnHandler(_roomManager)
        );

        _lobbyRegistry = new HandlerRegistry();
        _lobbyRegistry.Register(
            new CreateGameRoomHandler(_roomManager)
        );
    }

    public void PollEvents()
    {
        _netManager.PollEvents();
        _lobbyClient.PollEvents();
    }

    private void OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey(_clientKey);
    }

    private void OnPeerConnected(NetPeer peer)
    {
        Log.Information("[GameServer] 游戏客户端连接 endpoint={EndPoint}", peer.Address);
    }

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Log.Information("[GameServer] 游戏客户端断开 endpoint={EndPoint} reason={Reason}",
            peer.Address, disconnectInfo.Reason);
        _roomManager.RemovePlayer(peer);
    }

    private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            var (messageId, _, payload) = MessageHelper.ReadFrame(reader);
            _gameRegistry.Handle(peer, payload, messageId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GameServer] 游戏客户端消息处理异常");
        }
        finally
        {
            reader.Recycle();
        }
    }

    private void OnLobbyConnected(NetPeer peer)
    {
        _lobbyPeer = peer;
        Log.Information("[GameServer] 已连接到LobbyServer");

        MessageHelper.Send(peer, MessageIds.GameServerRegister, ReturnCode.Success, new GameServerInfo
        {
            Port = _serverPort,
            PlayerCount = 0,
            RoomCount = 0
        });
        Log.Information("[GameServer] 已向LobbyServer发送注册");
    }

    private void OnLobbyDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _lobbyPeer = null;
        Log.Warning("[GameServer] 断开LobbyServer连接 reason={Reason}", disconnectInfo.Reason);
    }

    private void OnLobbyReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            var (messageId, _, payload) = MessageHelper.ReadFrame(reader);
            _lobbyRegistry.Handle(peer, payload, messageId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GameServer] LobbyServer消息处理异常");
        }
        finally
        {
            reader.Recycle();
        }
    }

    private async Task UpdateLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _perf.Update();

            if (_lobbyPeer?.ConnectionState == ConnectionState.Connected)
            {
                SendGameServerUpdate();
            }
            else
            {
                Log.Warning("[GameServer] LobbyServer未连接，尝试重连");
                _lobbyClient.Connect(_lobbyAddress, _lobbyPort, _lobbyKey);
            }
        }
    }

    private void SendGameServerUpdate()
    {
        MessageHelper.Send(_lobbyPeer!, MessageIds.GameServerUpdate, ReturnCode.Success, new GameServerInfo
        {
            Port = _serverPort,
            PlayerCount = _roomManager.PlayerCount,
            RoomCount = _roomManager.RoomCount,
            CpuPercent = _perf.CpuPercent,
            MemoryMB = _perf.MemoryMB
        });
    }
}
