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
    private LobbyManager _lobbyManager = null!;
    private RoomManager _roomManager = null!;
    private HandlerRegistry _clientRegistry = null!;
    private HandlerRegistry _serverRegistry = null!;

    private readonly GameServerRegistry _gameServerRegistry = new();

    public LobbyServer(LobbyServerConfig config)
    {
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
        _roomManager = new RoomManager(_players, _gameServerRegistry);
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
            new RoomListHandler(_roomManager),
            new GameReadyHandler(_players, _roomManager),
            new GameUnreadyHandler(_players, _roomManager),
            new GameStartHandler(_players, _roomManager)
        );

        _serverRegistry.Register(
            new GameServerRegisterHandler(_gameServerRegistry),
            new GameServerUpdateHandler(_gameServerRegistry)
        );
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
        Log.Information("[LobbyServer] GameServer断开 endpoint={EndPoint} reason={Reason}",
            peer.Address, disconnectInfo.Reason);

        _gameServerRegistry.Remove(peer);
    }

    private void OnClientReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            var (messageId, _, payload) = MessageHelper.ReadFrame(reader);
            _clientRegistry.Handle(peer, payload, messageId);
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
            var (messageId, _, payload) = MessageHelper.ReadFrame(reader);
            _serverRegistry.Handle(peer, payload, messageId);
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
