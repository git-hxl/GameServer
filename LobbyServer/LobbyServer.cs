using LiteNetLib;
using LiteNetLib.Utils;
using Serilog;
using SharedLib.Config;
using System.Collections.Concurrent;
using SharedLib.Models;
using SharedLib.Protocol;
using LobbyServer.Lobby;
using LobbyServer.Player;
using LobbyServer.Room;
using LobbyServer.Handlers;

namespace LobbyServer;

public class LobbyServer
{
    private readonly NetManager _netManager;
    private readonly EventBasedNetListener _listener;
    private readonly string _connectionKey;
    private readonly PlayerManager _players = new();
    private LobbyManager _lobbyManager = null!;
    private RoomManager _roomManager = null!;
    private LobbyHandlerRegistry _registry = null!;

    private readonly ConcurrentDictionary<NetPeer, GameServerInfo> _gameServers = new();

    public LobbyServer(LobbyServerConfig config)
    {
        _connectionKey = config.ConnectionKey;
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener)
        {
            UpdateTime = config.UpdateTime,
            PingInterval = config.PingInterval,
            DisconnectTimeout = config.DisconnectTimeout,
            ChannelsCount = config.ChannelsCount
        };
    }

    public void Start(int port)
    {
        _lobbyManager = new LobbyManager(_netManager, _players);
        _roomManager = new RoomManager(_netManager, _players) { GameServers = _gameServers };
        _registry = new LobbyHandlerRegistry();

        RegisterHandlers();

        _listener.ConnectionRequestEvent += OnConnectionRequest;
        _listener.PeerConnectedEvent += OnPeerConnected;
        _listener.PeerDisconnectedEvent += OnPeerDisconnected;
        _listener.NetworkReceiveEvent += OnNetworkReceive;

        _netManager.Start(port);
        Log.Information("[LobbyServer] 大厅服务器启动 port={Port}", port);
    }

    private void RegisterHandlers()
    {
        _registry.Register(
            new JoinLobbyHandler(_lobbyManager),
            new LeaveLobbyHandler(_lobbyManager),
            new ChatHandler(_lobbyManager),
            new CreateRoomHandler(_players, _roomManager),
            new JoinRoomHandler(_players, _roomManager),
            new LeaveRoomHandler(_players, _roomManager),
            new RoomListHandler(_roomManager),
            new GameReadyHandler(_players, _roomManager),
            new GameUnreadyHandler(_players, _roomManager),
            new GameStartHandler(_players, _roomManager),
            new GameServerRegisterHandler(_gameServers),
            new GameServerUpdateHandler(_gameServers)
        );
    }

    public void PollEvents()
    {
        _netManager.PollEvents();
    }

    private void OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey(_connectionKey);
    }

    private void OnPeerConnected(NetPeer peer)
    {
        Log.Information("[LobbyServer] 客户端连接 endpoint={EndPoint}", peer.Address);
    }

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Log.Information("[LobbyServer] 客户端断开 endpoint={EndPoint} reason={Reason}",
            peer.Address, disconnectInfo.Reason);

        var player = _players.GetByPeer(peer);
        if (player != null)
        {
            var userId = player.Info.UserId;
            _roomManager.RemovePlayer(userId);
            _players.Unregister(userId);
        }

        _gameServers.TryRemove(peer, out _);
    }

    private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            var messageId = reader.GetUShort();
            reader.GetByte();
            var payload = reader.GetRemainingBytes();

            _registry.Handle(peer, payload ?? [], messageId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[LobbyServer] 消息处理异常");
        }
        finally
        {
            reader.Recycle();
        }
    }
}
