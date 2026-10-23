using LiteNetLib;
using Serilog;
using SharedLib.Config;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Redis;
using SharedLib.Utils;
using SharedLib.Handlers;
using LobbyServer.Cluster;
using LobbyServer.Lobby;
using LobbyServer.Player;
using LobbyServer.Room;
using LobbyServer.Handlers.Client;
using LobbyServer.Handlers.Server;

namespace LobbyServer;

public class LobbyServer
{
    private readonly NetManager _clientNetManager;
    private readonly EventBasedNetListener _clientListener;
    private readonly string _clientKey;

    private readonly NetManager _serverNetManager;
    private readonly EventBasedNetListener _serverListener;
    private readonly string _serverKey;

    private readonly PlayerManager _players = new();
    private readonly RedisHelper _redisHelper;
    private LobbyManager _lobbyManager = null!;
    private RoomManager _roomManager = null!;
    private HandlerRegistry _clientRegistry = null!;
    private HandlerRegistry _serverRegistry = null!;

    private readonly GameServerRegistry _gameServerRegistry = new();
    private bool _stopping;

    /// <summary>
    /// 创建大厅服务器实例；RedisHelper 由调用方创建，构造过程不连接 Redis。
    /// </summary>
    /// <param name="config">大厅服务器配置。</param>
    /// <param name="redis">进程级共享的 RedisHelper，由调用方负责释放。</param>
    public LobbyServer(LobbyServerConfig config, RedisHelper redis)
    {
        _redisHelper = redis;

        _clientKey = config.ClientConnectionKey;
        _serverKey = config.ServerConnectionKey;

        _clientListener = new EventBasedNetListener();
        _clientNetManager = new NetManager(_clientListener)
        {
            UpdateTime = config.UpdateTime,
            PingInterval = config.PingInterval,
            DisconnectTimeout = config.DisconnectTimeout,
            ChannelsCount = config.ChannelsCount
        };

        _serverListener = new EventBasedNetListener();
        _serverNetManager = new NetManager(_serverListener)
        {
            UpdateTime = config.UpdateTime,
            PingInterval = config.PingInterval,
            DisconnectTimeout = config.DisconnectTimeout,
            ChannelsCount = config.ChannelsCount
        };
    }

    /// <summary>
    /// 初始化大厅（创建房间存储、恢复房间）、注册 handler，并启动两个 NetManager。
    /// </summary>
    /// <param name="config">大厅服务器配置。</param>
    public void Start(LobbyServerConfig config)
    {
        _lobbyManager = LobbyManager.Create(
            _clientNetManager, _players, _gameServerRegistry, _redisHelper, config.RedisKeyPrefix);
        _roomManager = _lobbyManager.Rooms;

        _clientRegistry = new HandlerRegistry();
        _serverRegistry = new HandlerRegistry();

        RegisterHandlers();

        _clientListener.ConnectionRequestEvent += req => req.AcceptIfKey(_clientKey);
        _clientListener.PeerConnectedEvent += OnClientConnected;
        _clientListener.PeerDisconnectedEvent += OnClientDisconnected;
        _clientListener.NetworkReceiveEvent += OnClientReceive;

        _serverListener.ConnectionRequestEvent += req => req.AcceptIfKey(_serverKey);
        _serverListener.PeerConnectedEvent += OnServerConnected;
        _serverListener.PeerDisconnectedEvent += OnServerDisconnected;
        _serverListener.NetworkReceiveEvent += OnServerReceive;

        if (!_clientNetManager.Start(config.ClientPort))
            throw new InvalidOperationException($"LobbyServer 客户端端口启动失败: {config.ClientPort}");

        if (!_serverNetManager.Start(config.ServerPort))
        {
            _clientNetManager.Stop();
            throw new InvalidOperationException($"LobbyServer 服务端口启动失败: {config.ServerPort}");
        }

        Log.Information("[LobbyServer] 大厅服务器启动 clientPort={ClientPort} serverPort={ServerPort}",
            config.ClientPort, config.ServerPort);
    }

    private void RegisterHandlers()
    {
        _clientRegistry.Register(
            new JoinLobbyHandler(_lobbyManager),
            new LeaveLobbyHandler(_lobbyManager),
            new ChatHandler(_lobbyManager),
            new CreateRoomHandler(_players, _roomManager),
            new JoinRoomHandler(_players, _roomManager),
            new LeaveRoomHandler(_players, _roomManager),
            new RoomListHandler(_roomManager)
        );

        _serverRegistry.Register(
            new GameServerRegisterHandler(_gameServerRegistry, _roomManager),
            new GameServerUpdateHandler(_gameServerRegistry)
        );
    }

    /// <summary>服务端内部 API：由外部系统创建房间，首个加入者成为房主</summary>
    public (CreateRoomResponse Response, ReturnCode Code) CreateSystemRoom(CreateRoomRequest request)
    {
        return _roomManager.CreateRoom(request, null);
    }

    /// <summary>服务端内部 API：关闭房间并同步关闭游戏房间</summary>
    public ReturnCode CloseRoom(string roomId)
    {
        return _roomManager.CloseRoom(roomId, RoomCloseReason.ClosedBySystem);
    }

    /// <summary>
    /// 停止两个 NetManager；不会释放调用方注入的房间存储。
    /// </summary>
    public void Stop()
    {
        if (_stopping)
            return;

        _stopping = true;

        _clientNetManager.Stop();
        _serverNetManager.Stop();
    }

    public void PollEvents()
    {
        _clientNetManager.PollEvents();
        _serverNetManager.PollEvents();
    }

    private void OnClientConnected(NetPeer peer)
    {
        Log.Information("[LobbyServer] 客户端连接 endpoint={EndPoint}", peer.Address);
    }

    private void OnClientDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (_stopping)
            return;

        var player = _players.GetByPeer(peer);
        if (player == null)
            return;

        if (PlayerDisconnectPolicy.ShouldKeepPlayer(disconnectInfo.Reason))
        {
            _players.UnbindPeer(peer);
            Log.Information("[LobbyServer] 异常断线，保留玩家 userId={UserId} reason={Reason}",
                player.Info.UserId, disconnectInfo.Reason);
        }
        else
        {
            Log.Information("[LobbyServer] 主动断开，移除玩家 userId={UserId} reason={Reason}",
                player.Info.UserId, disconnectInfo.Reason);
            _roomManager.RemovePlayer(player.Info.UserId);
        }
    }

    private void OnServerConnected(NetPeer peer)
    {
        Log.Information("[LobbyServer] GameServer连接 endpoint={EndPoint}", peer.Address);
    }

    private void OnServerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (_stopping)
            return;

        Log.Information("[LobbyServer] GameServer断开 endpoint={EndPoint} reason={Reason}",
            peer.Address, disconnectInfo.Reason);

        _gameServerRegistry.Remove(peer);
    }

    private void OnClientReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            var frame = MessageHelper.ReadFrame(reader);
            _clientRegistry.Handle(peer, frame.MessageId, frame.Payload);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[LobbyServer] 客户端消息处理异常");
        }
        finally
        {
            reader.Recycle();
        }
    }

    private void OnServerReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            var frame = MessageHelper.ReadFrame(reader);
            _serverRegistry.Handle(peer, frame.MessageId, frame.Payload);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[LobbyServer] GameServer消息处理异常");
        }
        finally
        {
            reader.Recycle();
        }
    }
}
