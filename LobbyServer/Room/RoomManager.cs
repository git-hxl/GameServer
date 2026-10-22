using LiteNetLib;
using LobbyServer.Cluster;
using LobbyServer.Player;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;

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

    public (CreateRoomResponse Response, ReturnCode Code) CreateRoom(CreateRoomRequest request, long? creatorUserId)
    {
        Log.Information("[RoomManager] 创建房间 creator={Creator} roomId={RoomId}", creatorUserId, request.RoomId);

        if (creatorUserId.HasValue && _players.Get(creatorUserId.Value) == null)
        {
            Log.Warning("[RoomManager] 创建房间失败：玩家不在大厅 userId={UserId}", creatorUserId);
            return (new CreateRoomResponse(), ReturnCode.NotInLobby);
        }

        var roomId = string.IsNullOrWhiteSpace(request.RoomId)
            ? Guid.NewGuid().ToString("N")[..8]
            : request.RoomId;

        if (_rooms.ContainsKey(roomId))
        {
            Log.Warning("[RoomManager] 创建房间失败：房间已存在 roomId={RoomId}", roomId);
            return (new CreateRoomResponse(), ReturnCode.Error);
        }

        var gs = PickGameServer();
        if (gs == null)
        {
            Log.Warning("[RoomManager] 创建房间失败：无可用GameServer");
            return (new CreateRoomResponse(), ReturnCode.NoGameServerAvailable);
        }

        var gsValue = gs.Value;

        try
        {
            MessageHelper.Send(gsValue.Key, MessageIds.CreateGameRoom, ReturnCode.Success, new CreateGameRoomRequest
            {
                RoomId = roomId,
                RoomType = request.RoomType,
                MaxPlayers = request.MaxPlayers,
                OwnerUserId = creatorUserId ?? 0
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[RoomManager] 同步创建游戏房间失败 roomId={RoomId}", roomId);
            return (new CreateRoomResponse(), ReturnCode.GameServerOffline);
        }

        if (creatorUserId.HasValue && _players.Get(creatorUserId.Value)?.CurrentRoomId != null)
            LeaveRoom(creatorUserId.Value);

        var room = new LobbyRoom
        {
            RoomId = roomId,
            RoomType = request.RoomType,
            MaxPlayers = request.MaxPlayers,
            OwnerUserId = creatorUserId ?? 0,
            CreatorUserId = creatorUserId,
            GameServerAddress = gsValue.Value.Address,
            GameServerPort = gsValue.Value.Port
        };

        if (creatorUserId.HasValue)
        {
            room.PlayerIds.Add(creatorUserId.Value);
            _players.SetState(creatorUserId.Value, PlayerState.InRoom, roomId);
        }

        _rooms[roomId] = room;

        Log.Information(
            "[RoomManager] 房间创建成功 roomId={RoomId} type={RoomType} owner={Owner} GameServer={Addr}:{Port} 成员数={Count}",
            roomId, room.RoomType, room.OwnerUserId, room.GameServerAddress, room.GameServerPort,
            room.PlayerIds.Count);

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
            Log.Warning("[RoomManager] 加入房间失败：房间未找到 roomId={RoomId}", request.RoomId);
            return (new JoinRoomResponse { Room = new RoomInfo { RoomId = request.RoomId } }, ReturnCode.RoomNotFound);
        }

        if (room.MaxPlayers > 0 && room.PlayerIds.Count >= room.MaxPlayers)
        {
            Log.Warning("[RoomManager] 加入房间失败：房间已满 roomId={RoomId} max={Max}",
                request.RoomId, room.MaxPlayers);
            return (new JoinRoomResponse { Room = new RoomInfo { RoomId = request.RoomId } }, ReturnCode.RoomFull);
        }

        if (room.PlayerIds.Contains(userId))
        {
            Log.Warning("[RoomManager] 加入房间失败：已在房间中 roomId={RoomId} userId={UserId}",
                request.RoomId, userId);
            return (new JoinRoomResponse { Room = BuildRoomInfo(room) }, ReturnCode.AlreadyInRoom);
        }

        if (player.CurrentRoomId != null)
            LeaveRoom(userId);

        room.PlayerIds.Add(userId);
        if (room.OwnerUserId == 0)
            room.OwnerUserId = userId;

        _players.SetState(userId, PlayerState.InRoom, request.RoomId);

        var notify = new JoinRoomNotify { RoomId = room.RoomId, Player = player.Info };
        foreach (var otherId in room.PlayerIds.Where(id => id != userId))
        {
            var otherPlayer = _players.Get(otherId);
            if (otherPlayer != null)
                MessageHelper.Send(otherPlayer.Peer, MessageIds.JoinRoomNotify, ReturnCode.Success, notify);
        }

        Log.Information("[RoomManager] 加入房间成功 roomId={RoomId} userId={UserId} 房主={Owner} 房间人数={Count}",
            room.RoomId, userId, room.OwnerUserId, room.PlayerIds.Count);

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

        _players.SetState(userId, PlayerState.InLobby);

        var remaining = room.PlayerIds.Count;
        Log.Information("[RoomManager] 离开房间 roomId={RoomId} userId={UserId} 剩余人数={Count}",
            roomId, userId, remaining);

        if (remaining == 0)
        {
            _rooms.Remove(roomId);
            SendCloseGameRoom(room, RoomCloseReason.Empty);
            Log.Information("[RoomManager] 房间关闭 roomId={RoomId}（无玩家）", roomId);
        }
        else
        {
            if (room.OwnerUserId == userId)
            {
                room.OwnerUserId = room.PlayerIds[0];
                Broadcast(room, MessageIds.RoomOwnerChangedNotify, new RoomOwnerChangedNotify
                {
                    RoomId = roomId,
                    OwnerUserId = room.OwnerUserId
                });
                Log.Information("[RoomManager] 房主转移 roomId={RoomId} →{NewOwner}", roomId, room.OwnerUserId);
            }

            Broadcast(room, MessageIds.LeaveRoomNotify, new LeaveRoomNotify
            {
                RoomId = roomId,
                UserId = userId
            });
        }

        return (new LeaveRoomResponse { RoomId = roomId }, ReturnCode.Success);
    }

    public ReturnCode CloseRoom(string roomId, RoomCloseReason reason)
    {
        if (!_rooms.Remove(roomId, out var room))
        {
            Log.Warning("[RoomManager] 关闭房间失败：房间未找到 roomId={RoomId}", roomId);
            return ReturnCode.RoomNotFound;
        }

        var frame = MessageHelper.CreateFrame(MessageIds.RoomClosedNotify, ReturnCode.Success,
            new RoomClosedNotify { RoomId = roomId, Reason = reason });

        var peers = new List<NetPeer>();
        foreach (var userId in room.PlayerIds)
        {
            var player = _players.Get(userId);
            if (player == null)
                continue;

            peers.Add(player.Peer);
            _players.SetState(userId, PlayerState.InLobby);
        }

        MessageHelper.SendToAll(peers, frame, DeliveryMethod.ReliableOrdered);
        SendCloseGameRoom(room, reason);

        Log.Information("[RoomManager] 房间关闭 roomId={RoomId} reason={Reason} 成员数={Count}",
            roomId, reason, peers.Count);
        return ReturnCode.Success;
    }

    public RoomListResponse GetRoomList()
    {
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

    private void Broadcast(LobbyRoom room, ushort messageId, object data)
    {
        var frame = MessageHelper.CreateFrame(messageId, ReturnCode.Success, data);
        var peers = room.PlayerIds
            .Select(id => _players.Get(id))
            .Where(p => p != null)
            .Select(p => p!.Peer);

        MessageHelper.SendToAll(peers, frame, DeliveryMethod.ReliableOrdered);
    }

    private void SendCloseGameRoom(LobbyRoom room, RoomCloseReason reason)
    {
        var gs = _gameServerRegistry.GetByAddressPort(room.GameServerAddress, room.GameServerPort);
        if (gs == null)
        {
            Log.Warning("[RoomManager] GameServer离线，跳过关闭游戏房间 roomId={RoomId}", room.RoomId);
            return;
        }

        try
        {
            MessageHelper.Send(gs.Peer, MessageIds.CloseGameRoom, ReturnCode.Success, new CloseGameRoomRequest
            {
                RoomId = room.RoomId,
                Reason = reason
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[RoomManager] 关闭游戏房间失败 roomId={RoomId}", room.RoomId);
        }
    }

    private RoomInfo BuildRoomInfo(LobbyRoom room)
    {
        var gs = _gameServerRegistry.GetByAddressPort(room.GameServerAddress, room.GameServerPort);

        return new RoomInfo
        {
            RoomId = room.RoomId,
            RoomType = room.RoomType,
            GameServerAddress = gs?.Info.Address ?? room.GameServerAddress,
            GameServerPort = gs?.Info.Port ?? room.GameServerPort,
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
}
