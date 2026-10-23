using LiteNetLib;
using LiteNetLib.Utils;
using Serilog;
using SharedLib.Config;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using LobbyServer.Cluster;
using LobbyServer.Lobby;
using LobbyServer.Persistence;
using LobbyServer.Player;
using LobbyServer.Room;
using LobbyServer.Handlers;

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
    private readonly ILobbyRoomStore _roomStore;
    private readonly bool _ownsRoomStore;
    private LobbyManager _lobbyManager = null!;
    private RoomManager _roomManager = null!;
    private HandlerRegistry _clientRegistry = null!;
    private HandlerRegistry _serverRegistry = null!;

    private readonly GameServerRegistry _gameServerRegistry = new();
    private bool _stopping;

    public LobbyServer(LobbyServerConfig config, ILobbyRoomStore? roomStore = null)
    {
        _ownsRoomStore = roomStore == null;
        _roomStore = roomStore ?? new RedisLobbyRoomStore(
            config.RedisConnection, config.RedisKeyPrefix, config.RedisDatabase);

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

    public void Start(LobbyServerConfig config)
    {
        _roomManager = new RoomManager(_players, _gameServerRegistry, _roomStore);
        _roomManager.RestoreFromStore();
        _lobbyManager = new LobbyManager(_clientNetManager, _players, _roomManager);

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

        _clientNetManager.Start(config.ClientPort);
        _serverNetManager.Start(config.ServerPort);
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

    public void Stop()
    {
        if (_stopping)
            return;

        _stopping = true;

        _clientNetManager.Stop();
        _serverNetManager.Stop();

        if (_ownsRoomStore)
            _roomStore.Dispose();
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

        Log.Information("[LobbyServer] 客户端断开 endpoint={EndPoint} reason={Reason}",
            peer.Address, disconnectInfo.Reason);

        _roomManager.RemovePlayer(peer);
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
