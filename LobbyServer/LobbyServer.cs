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

    /// <summary>
    /// 处理客户端断开：Timeout 保留玩家和房间身份；其他原因移出房间，主动断线删除 LobbyPlayer。
    /// </summary>
    /// <param name="peer">断开的连接。</param>
    /// <param name="disconnectInfo">断开原因。</param>
    private void OnClientDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (_stopping)
            return;

        var player = _players.GetByPeer(peer);
        if (player == null)
            return;

        var userId = player.Info.UserId;

        // Timeout：只清 Peer，保留 LobbyPlayer 和房间成员身份，便于原房重连。
        if (disconnectInfo.Reason == DisconnectReason.Timeout)
        {
            _players.UnbindPeer(peer);
            Log.Information("[LobbyServer] 超时断线，保留玩家和房间身份 userId={UserId}", userId);
            return;
        }

        // 其他原因：先移出房间（房间/Redis 同步更新）。
        _roomManager.LeaveRoom(userId);

        if (disconnectInfo.Reason == DisconnectReason.RemoteConnectionClose)
        {
            _players.Remove(userId);
            Log.Information("[LobbyServer] 主动断线，移出房间并删除玩家 userId={UserId} reason={Reason}",
                userId, disconnectInfo.Reason);
        }
        else
        {
            _players.UnbindPeer(peer);
            Log.Information("[LobbyServer] 断线，移出房间并保留离线玩家 userId={UserId} reason={Reason}",
                userId, disconnectInfo.Reason);
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
