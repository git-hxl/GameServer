using LiteNetLib;
using LobbyServer.Cluster;
using LobbyServer.Persistence;
using LobbyServer.Player;
using LobbyServer.Room;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Redis;
using SharedLib.Utils;

namespace LobbyServer.Lobby;

public class LobbyManager
{
    private readonly NetManager _netManager;
    private readonly PlayerManager _players;
    private readonly RoomManager _rooms;

    public int UserCount => _players.OnlineCount;

    /// <summary>
    /// 大厅房间管理器，供 handler 注册和外部 API 使用。
    /// </summary>
    public RoomManager Rooms => _rooms;

    /// <summary>
    /// 创建 LobbyManager；只允许通过 <see cref="Create"/> 完成 Redis 存储和房间恢复。
    /// </summary>
    /// <param name="netManager">Lobby 客户端 NetManager。</param>
    /// <param name="players">玩家管理器。</param>
    /// <param name="rooms">已恢复房间的 RoomManager。</param>
    private LobbyManager(NetManager netManager, PlayerManager players, RoomManager rooms)
    {
        _netManager = netManager;
        _players = players;
        _rooms = rooms;
    }

    /// <summary>
    /// 初始化大厅：创建 Redis 房间存储、RoomManager，并从存储恢复房间。
    /// </summary>
    /// <param name="netManager">Lobby 客户端 NetManager。</param>
    /// <param name="players">玩家管理器。</param>
    /// <param name="gameServerRegistry">GameServer 注册表。</param>
    /// <param name="redis">进程级共享的 RedisHelper。</param>
    /// <param name="keyPrefix">Redis key 前缀。</param>
    /// <returns>已完成房间恢复的 LobbyManager。</returns>
    public static LobbyManager Create(
        NetManager netManager,
        PlayerManager players,
        GameServerRegistry gameServerRegistry,
        RedisHelper redis,
        string keyPrefix)
    {
        var roomStore = new RedisLobbyRoomStore(redis, keyPrefix);
        var rooms = new RoomManager(players, gameServerRegistry, roomStore);
        rooms.RestoreFromStore();

        return new LobbyManager(netManager, players, rooms);
    }

    public (JoinLobbyResponse Response, ReturnCode Code) Join(NetPeer peer, JoinLobbyRequest request)
    {
        var userId = request.Player.UserId;
        Log.Information("[LobbyManager] 加入大厅 userId={UserId}", userId);

        var boundUserId = _players.GetUserId(peer);
        if (boundUserId != 0 && boundUserId != userId)
        {
            Log.Warning("[LobbyManager] 同一连接切换 userId old={Old} new={New}", boundUserId, userId);
            _rooms.RemovePlayer(boundUserId);
        }

        var existing = _players.Get(userId);
        if (existing != null)
        {
            Log.Warning("[LobbyManager] 玩家重新登录 userId={UserId} online={Online}", userId, existing.Peer != null);

            var oldPeer = existing.Peer;
            if (oldPeer != null && oldPeer != peer)
            {
                _players.UnbindPeer(oldPeer);
                _netManager.DisconnectPeer(oldPeer);
            }

            _players.BindOnline(existing, peer, request.Player);
            _rooms.PersistPlayerRoom(userId);

            Log.Information("[LobbyManager] 玩家恢复会话 userId={UserId} roomId={RoomId}",
                userId, existing.CurrentRoomId);

            return (new JoinLobbyResponse { Player = request.Player }, ReturnCode.Success);
        }

        _players.Add(peer, request.Player);

        Log.Information("[LobbyManager] 大厅加入 userId={UserId} nickname={Nickname} 在线人数={Count}",
            userId, request.Player.Nickname, _players.OnlineCount);

        return (new JoinLobbyResponse { Player = request.Player }, ReturnCode.Success);
    }

    public (LeaveLobbyResponse Response, ReturnCode Code) Leave(NetPeer peer, LeaveLobbyRequest request)
    {
        Log.Information("[LobbyManager] 离开大厅 userId={UserId}", request.UserId);

        var player = _players.Get(request.UserId);
        if (player == null)
        {
            Log.Warning("[LobbyManager] 离开大厅失败：用户不在大厅中 userId={UserId}", request.UserId);
            return (new LeaveLobbyResponse { UserId = request.UserId }, ReturnCode.NotInLobby);
        }

        if (player.Peer != peer)
        {
            Log.Warning("[LobbyManager] 离开大厅失败：Peer不匹配 userId={UserId}", request.UserId);
            return (new LeaveLobbyResponse { UserId = request.UserId }, ReturnCode.NotInLobby);
        }

        _rooms.RemovePlayer(request.UserId);

        Log.Information("[LobbyManager] 大厅离开 userId={UserId} 在线人数={Count}",
            request.UserId, _players.OnlineCount);

        return (new LeaveLobbyResponse { UserId = request.UserId }, ReturnCode.Success);
    }

    public void Chat(NetPeer peer, ChatRequest request)
    {
        Log.Information("[LobbyManager] 聊天消息 userId={UserId}", request.UserId);

        var player = _players.Get(request.UserId);
        if (player == null || player.Peer != peer)
        {
            Log.Warning("[LobbyManager] 聊天被拒绝：用户不在大厅中 userId={UserId}", request.UserId);
            return;
        }

        Log.Information("[LobbyManager] 聊天 userId={UserId} nickname={Nickname} content={Content}",
            request.UserId, request.Nickname, request.Content);

        Broadcast(MessageIds.ChatNotify, ReturnCode.Success, new ChatNotify
        {
            UserId = request.UserId,
            Nickname = request.Nickname,
            Content = request.Content
        });
    }

    private void Broadcast(ushort messageId, ReturnCode code, object data)
    {
        var frame = MessageHelper.CreateFrame(messageId, code, data);
        MessageHelper.SendToAll(_players.OnlinePeers, frame, DeliveryMethod.ReliableOrdered);
    }
}
