using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using LobbyServer.Cluster;
using LobbyServer.Player;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;

namespace LobbyServer.Room;

public class RoomManager
{
    private readonly PlayerManager _players;
    private readonly GameServerRegistry _gameServerRegistry;
    private readonly Dictionary<string, LobbyRoom> _rooms = new();

    public RoomManager(PlayerManager players, GameServerRegistry gameServerRegistry)
    {
        _players = players;
        _gameServerRegistry = gameServerRegistry;
    }

    public (CreateRoomResponse Response, ReturnCode Code) CreateRoom(long userId, CreateRoomRequest request)
    {
        Log.Information("[RoomManager] 创建房间 userId={UserId} roomId={RoomId}", userId, request.RoomId);

        var player = _players.Get(userId);
        if (player == null)
        {
            Log.Warning("[RoomManager] 创建房间失败：玩家不存在 userId={UserId}", userId);
            return (new CreateRoomResponse(), ReturnCode.NotInLobby);
        }

        var gs = PickGameServer();
        if (gs == null)
        {
            Log.Warning("[RoomManager] 创建房间失败：无可用GameServer userId={UserId}", userId);
            return (new CreateRoomResponse(), ReturnCode.NoGameServerAvailable);
        }

        var gsValue = gs.Value;
        var roomId = request.RoomId ?? Guid.NewGuid().ToString("N")[..8];

        if (_rooms.ContainsKey(roomId))
        {
            Log.Warning("[RoomManager] 创建房间失败：房间已存在 roomId={RoomId}", roomId);
            return (new CreateRoomResponse(), ReturnCode.Error);
        }

        var room = new LobbyRoom
        {
            RoomId = roomId,
            RoomType = request.RoomType,
            OwnerUserId = userId,
            GameServerPeer = gsValue.Key,
            PlayerIds = new HashSet<long> { userId }
        };

        _rooms[roomId] = room;
        _players.SetState(userId, PlayerState.InRoom, roomId);

        Log.Information("[RoomManager] 房间创建成功 roomId={RoomId} GameServer={Addr}:{Port} 房主={UserId} 总房间数={TotalRooms}",
            roomId, gsValue.Value.Address, gsValue.Value.Port, userId, _rooms.Count);
        return (new CreateRoomResponse { Room = BuildRoomInfo(room) }, ReturnCode.Success);
    }

    public (JoinRoomResponse Response, ReturnCode Code) JoinRoom(long userId, JoinRoomRequest request)
    {
        Log.Information("[RoomManager] 加入房间 userId={UserId} roomId={RoomId}", userId, request.RoomId);

        var player = _players.Get(userId);
        if (player == null)
        {
            Log.Warning("[RoomManager] 加入房间失败：玩家不存在 userId={UserId}", userId);
            return (new JoinRoomResponse { Room = new RoomInfo { RoomId = request.RoomId } }, ReturnCode.NotInLobby);
        }

        if (!_rooms.TryGetValue(request.RoomId, out var room))
        {
            Log.Warning("[RoomManager] 加入房间失败：房间未找到 roomId={RoomId} userId={UserId}", request.RoomId, userId);
            return (new JoinRoomResponse { Room = new RoomInfo { RoomId = request.RoomId } }, ReturnCode.RoomNotFound);
        }

        if (room.PlayerIds.Contains(userId))
        {
            Log.Warning("[RoomManager] 加入房间失败：已在房间中 roomId={RoomId} userId={UserId}", request.RoomId, userId);
            return (new JoinRoomResponse { Room = new RoomInfo { RoomId = request.RoomId } }, ReturnCode.AlreadyInRoom);
        }

        LeaveRoom(userId);

        room.PlayerIds.Add(userId);
        room.OwnerUserId = _players.Get(room.OwnerUserId)?.Info.UserId ?? 0;

        _players.SetState(userId, PlayerState.InRoom, request.RoomId);

        var notify = new JoinRoomNotify { RoomId = request.RoomId, Player = player.Info };
        foreach (var otherId in room.PlayerIds.Where(id => id != userId))
        {
            var otherPlayer = _players.Get(otherId);
            if (otherPlayer != null)
                Send(otherPlayer.Peer, MessageIds.JoinRoomNotify, ReturnCode.Success, notify);
        }

        Log.Information("[RoomManager] 加入房间成功 roomId={RoomId} userId={UserId} 房主={Owner} 房间人数={Count}",
            request.RoomId, userId, room.OwnerUserId, room.PlayerIds.Count);
        return (new JoinRoomResponse { Room = BuildRoomInfo(room) }, ReturnCode.Success);
    }

    public (LeaveRoomResponse Response, ReturnCode Code) LeaveRoom(long userId)
    {
        Log.Information("[RoomManager] 离开房间 userId={UserId}", userId);

        var player = _players.Get(userId);
        if (player == null || player.CurrentRoomId == null)
            return (new LeaveRoomResponse(), ReturnCode.NotInRoom);

        var roomId = player.CurrentRoomId;
        if (!_rooms.TryGetValue(roomId, out var room))
            return (new LeaveRoomResponse(), ReturnCode.NotInRoom);

        if (!room.PlayerIds.Remove(userId))
            return (new LeaveRoomResponse(), ReturnCode.NotInRoom);

        room.ReadyPlayerIds.Remove(userId);
        _players.SetState(userId, PlayerState.InLobby);

        var remaining = room.PlayerIds.Count;
        Log.Information("[RoomManager] 离开房间 roomId={RoomId} userId={UserId} 剩余人数={Count}",
            roomId, userId, remaining);

        if (remaining == 0)
        {
            _rooms.Remove(roomId);
            Log.Information("[RoomManager] 房间关闭 roomId={RoomId}（无玩家）", roomId);
        }
        else
        {
            ReassignOwner(roomId, room);
            var leaveNotify = new LeaveRoomNotify { RoomId = roomId, UserId = userId };
            foreach (var otherId in room.PlayerIds)
            {
                var otherPlayer = _players.Get(otherId);
                if (otherPlayer != null)
                    Send(otherPlayer.Peer, MessageIds.LeaveRoomNotify, ReturnCode.Success, leaveNotify);
            }
        }

        return (new LeaveRoomResponse { RoomId = roomId }, ReturnCode.Success);
    }

    public (GameReadyResponse Response, ReturnCode Code) SetReady(long userId)
    {
        var player = _players.Get(userId);
        if (player == null || player.CurrentRoomId == null)
            return (new GameReadyResponse(), ReturnCode.NotInRoom);

        var roomId = player.CurrentRoomId;
        if (!_rooms.TryGetValue(roomId, out var room))
            return (new GameReadyResponse(), ReturnCode.NotInRoom);

        room.ReadyPlayerIds.Add(userId);
        _players.SetState(userId, PlayerState.Ready);

        var ready = room.ReadyPlayerIds.Count;
        var total = room.PlayerIds.Count;
        Log.Information("[RoomManager] 玩家准备 roomId={RoomId} userId={UserId} ready={Ready}/{Total}",
            roomId, userId, ready, total);

        var notify = new GameReadyNotify
        {
            RoomId = roomId,
            UserId = userId,
            IsReady = true,
            ReadyCount = ready,
            TotalCount = total
        };
        foreach (var otherId in room.PlayerIds.Where(id => id != userId))
        {
            var otherPlayer = _players.Get(otherId);
            if (otherPlayer != null)
                Send(otherPlayer.Peer, MessageIds.GameReadyNotify, ReturnCode.Success, notify);
        }

        var allReady = ready >= total;
        return (new GameReadyResponse { ReadyCount = ready, TotalCount = total, AllReady = allReady }, ReturnCode.Success);
    }

    public (GameUnreadyResponse Response, ReturnCode Code) SetUnready(long userId)
    {
        var player = _players.Get(userId);
        if (player == null || player.CurrentRoomId == null)
            return (new GameUnreadyResponse(), ReturnCode.NotInRoom);

        var roomId = player.CurrentRoomId;
        if (!_rooms.TryGetValue(roomId, out var room))
            return (new GameUnreadyResponse(), ReturnCode.NotInRoom);

        room.ReadyPlayerIds.Remove(userId);
        _players.SetState(userId, PlayerState.InRoom);

        var ready = room.ReadyPlayerIds.Count;
        var total = room.PlayerIds.Count;
        Log.Information("[RoomManager] 玩家取消准备 roomId={RoomId} userId={UserId} ready={Ready}/{Total}",
            roomId, userId, ready, total);

        var notify = new GameReadyNotify
        {
            RoomId = roomId,
            UserId = userId,
            IsReady = false,
            ReadyCount = ready,
            TotalCount = total
        };
        foreach (var otherId in room.PlayerIds.Where(id => id != userId))
        {
            var otherPlayer = _players.Get(otherId);
            if (otherPlayer != null)
                Send(otherPlayer.Peer, MessageIds.GameReadyNotify, ReturnCode.Success, notify);
        }

        return (new GameUnreadyResponse { ReadyCount = ready, TotalCount = total }, ReturnCode.Success);
    }

    public (ReturnCode Code, GameStartNotify? Notify) StartGame(long userId)
    {
        var player = _players.Get(userId);
        if (player == null || player.CurrentRoomId == null)
            return (ReturnCode.NotInRoom, null);

        var roomId = player.CurrentRoomId;
        if (!_rooms.TryGetValue(roomId, out var room))
            return (ReturnCode.NotInRoom, null);

        if (room.OwnerUserId != userId)
        {
            Log.Warning("[RoomManager] 开始游戏失败：不是房主 roomId={RoomId}", roomId);
            return (ReturnCode.NotRoomOwner, null);
        }

        if (room.ReadyPlayerIds.Count < room.PlayerIds.Count)
        {
            Log.Warning("[RoomManager] 开始游戏失败：玩家未全部准备 roomId={RoomId} ready={Ready}/{Total}",
                roomId, room.ReadyPlayerIds.Count, room.PlayerIds.Count);
            return (ReturnCode.NotAllReady, null);
        }

        var gs = _gameServerRegistry.Get(room.GameServerPeer);
        if (gs == null)
        {
            Log.Warning("[RoomManager] 开始游戏失败：GameServer已离线 roomId={RoomId}", roomId);
            return (ReturnCode.NoGameServerAvailable, null);
        }

        Send(room.GameServerPeer, MessageIds.CreateGameRoom, ReturnCode.Success, new CreateGameRoomRequest
        {
            RoomId = roomId,
            RoomType = room.RoomType,
            OwnerUserId = room.OwnerUserId
        });

        var notify = new GameStartNotify
        {
            RoomId = roomId,
            GameServerAddress = gs.Address,
            GameServerPort = gs.Port
        };

        foreach (var id in room.PlayerIds)
        {
            var p = _players.Get(id);
            if (p != null)
            {
                _players.SetState(id, PlayerState.InGame);
                Send(p.Peer, MessageIds.GameStartNotify, ReturnCode.Success, notify);
            }
        }

        room.ReadyPlayerIds.Clear();

        Log.Information("[RoomManager] 游戏开始 roomId={RoomId} GameServer={Addr}:{Port}",
            roomId, gs.Address, gs.Port);
        return (ReturnCode.Success, notify);
    }

    public RoomListResponse GetRoomList()
    {
        Log.Information("[RoomManager] 获取房间列表");

        var list = _rooms.Values.Select(r => new RoomListInfo
        {
            RoomId = r.RoomId,
            RoomType = r.RoomType,
            PlayerCount = r.PlayerIds.Count
        }).ToList();

        Log.Information("[RoomManager] 查询房间列表 房间数={Count}", list.Count);
        return new RoomListResponse { Rooms = list };
    }

    public void RemovePlayer(NetPeer peer)
    {
        var player = _players.GetByPeer(peer);
        if (player == null)
            return;

        var userId = player.Info.UserId;
        Log.Information("[RoomManager] 移除玩家 userId={UserId}", userId);
        LeaveRoom(userId);
        _players.Remove(userId);
    }

    private void ReassignOwner(string roomId, LobbyRoom room)
    {
        Log.Information("[RoomManager] 重新分配房主 roomId={RoomId}", roomId);

        var newOwnerId = room.PlayerIds.FirstOrDefault();
        room.OwnerUserId = newOwnerId;

        if (room.OwnerUserId != 0)
        {
            Log.Information("[RoomManager] 房主转移 roomId={RoomId} →{NewOwner}",
                roomId, room.OwnerUserId);
        }
    }

    private RoomInfo BuildRoomInfo(LobbyRoom room)
    {
        var gs = _gameServerRegistry.Get(room.GameServerPeer);

        return new RoomInfo
        {
            RoomId = room.RoomId,
            RoomType = room.RoomType,
            GameServerAddress = gs?.Address ?? string.Empty,
            GameServerPort = gs?.Port ?? 0,
            OwnerUserId = room.OwnerUserId,
            Players = room.PlayerIds
                .Select(id => _players.Get(id))
                .Where(p => p != null)
                .Select(p => p!.Info)
                .ToList()
        };
    }

    private KeyValuePair<NetPeer, GameServerInfo>? PickGameServer()
    {
        Log.Information("[RoomManager] 选择GameServer");

        var result = _gameServerRegistry.PickLeastLoaded();

        if (result is { } gs)
        {
            Log.Information("[RoomManager] 选择 GameServer {Addr}:{Port} 负载={Players}",
                gs.Value.Address, gs.Value.Port, gs.Value.PlayerCount);
        }
        else
        {
            Log.Warning("[RoomManager] 选择GameServer失败：无可用GameServer");
        }

        return result;
    }

    private void Send(NetPeer peer, ushort messageId, ReturnCode code, object data)
    {
        var writer = new NetDataWriter();
        writer.Put(messageId);
        writer.Put((byte)code);
        writer.Put(MessagePackSerializer.Serialize(data));
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
}
