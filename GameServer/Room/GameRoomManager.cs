using System.Collections.Concurrent;
using LiteNetLib;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using GameServer.Player;

namespace GameServer.Room;

public class GameRoomManager
{
    private readonly GamePlayerManager _playerManager;
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    public int PlayerCount => _playerManager.Count;

    public int RoomCount => _rooms.Count;

    public GameRoomManager(GamePlayerManager playerManager)
    {
        _playerManager = playerManager;
    }

    public GameRoom? GetRoom(string roomId)
    {
        return _rooms.TryGetValue(roomId, out var room) ? room : null;
    }

    public void CreateRoom(CreateGameRoomRequest request)
    {
        var room = new GameRoom
        {
            RoomId = request.RoomId,
            RoomType = request.RoomType,
            OwnerUserId = request.OwnerUserId,
            MaxPlayers = request.MaxPlayers
        };

        if (!_rooms.TryAdd(request.RoomId, room))
        {
            Log.Warning("[GameRoomManager] 游戏房间已存在，忽略重复创建 roomId={RoomId}", request.RoomId);
            return;
        }

        Log.Information("[GameRoomManager] 游戏房间创建 roomId={RoomId} type={RoomType} maxPlayers={Max}",
            request.RoomId, request.RoomType, request.MaxPlayers);
    }

    public void CloseRoom(CloseGameRoomRequest request)
    {
        if (!_rooms.TryRemove(request.RoomId, out var room))
        {
            Log.Warning("[GameRoomManager] 关闭游戏房间失败：房间未找到 roomId={RoomId}", request.RoomId);
            return;
        }

        var notify = new RoomClosedNotify { RoomId = request.RoomId, Reason = request.Reason };
        foreach (var userId in room.PlayerIds)
        {
            var player = _playerManager.Get(userId);
            if (player == null)
                continue;

            MessageHelper.Send(player.Peer, MessageIds.RoomClosedNotify, ReturnCode.Success, notify);
            _playerManager.Remove(userId);
        }

        Log.Information("[GameRoomManager] 游戏房间关闭 roomId={RoomId} reason={Reason} 移出玩家={Count}",
            request.RoomId, request.Reason, room.PlayerIds.Count);
    }

    public (JoinGameResponse Response, ReturnCode Code) JoinGame(NetPeer peer, JoinGameRequest request)
    {
        if (!_rooms.TryGetValue(request.RoomId, out var room))
        {
            Log.Warning("[GameRoomManager] 加入游戏失败：房间未找到 roomId={RoomId}", request.RoomId);
            return (new JoinGameResponse(), ReturnCode.RoomNotFound);
        }

        if (_playerManager.GetByPeer(peer) != null)
        {
            Log.Warning("[GameRoomManager] 加入游戏失败：连接已在游戏房间中 endpoint={EndPoint}", peer.Address);
            return (new JoinGameResponse(), ReturnCode.AlreadyInRoom);
        }

        var userId = request.Player.UserId;
        if (room.PlayerIds.Contains(userId))
        {
            Log.Warning("[GameRoomManager] 加入游戏失败：玩家已在房间中 userId={UserId} roomId={RoomId}",
                userId, request.RoomId);
            return (new JoinGameResponse(), ReturnCode.AlreadyInRoom);
        }

        var player = _playerManager.Add(peer, request.Player);
        player.CurrentRoomId = request.RoomId;
        room.PlayerIds.Add(userId);

        var notify = new JoinGameNotify { RoomId = request.RoomId, Player = player.Info };
        foreach (var otherId in room.PlayerIds.Where(id => id != userId))
        {
            var other = _playerManager.Get(otherId);
            if (other != null)
                MessageHelper.Send(other.Peer, MessageIds.JoinGameNotify, ReturnCode.Success, notify);
        }

        Log.Information("[GameRoomManager] 玩家加入游戏房间 roomId={RoomId} userId={UserId} 当前人数={Count}",
            request.RoomId, userId, room.PlayerIds.Count);

        return (new JoinGameResponse
        {
            RoomId = request.RoomId,
            RoomType = room.RoomType,
            OwnerUserId = room.OwnerUserId,
            Players = room.PlayerIds
                .Select(id => _playerManager.Get(id))
                .Where(p => p != null)
                .Select(p => p!.Info)
                .ToList()
        }, ReturnCode.Success);
    }

    public (ReturnCode Code, string? RoomId) LeaveGame(NetPeer peer)
    {
        return LeaveCore(peer, "离开");
    }

    public void RemovePlayer(NetPeer peer)
    {
        LeaveCore(peer, "断线离开");
    }

    private (ReturnCode Code, string? RoomId) LeaveCore(NetPeer peer, string reason)
    {
        var player = _playerManager.GetByPeer(peer);
        if (player == null || player.CurrentRoomId == null)
            return (ReturnCode.NotInRoom, null);

        var roomId = player.CurrentRoomId;
        if (!_rooms.TryGetValue(roomId, out var room))
            return (ReturnCode.NotInRoom, null);

        room.PlayerIds.Remove(player.Info.UserId);
        _playerManager.Remove(player.Info.UserId);

        var notify = new LeaveGameNotify { UserId = player.Info.UserId };
        foreach (var otherId in room.PlayerIds)
        {
            var other = _playerManager.Get(otherId);
            if (other != null)
                MessageHelper.Send(other.Peer, MessageIds.LeaveGameNotify, ReturnCode.Success, notify);
        }

        Log.Information("[GameRoomManager] 玩家{Reason}游戏房间 roomId={RoomId} 剩余人数={Count}",
            reason, roomId, room.PlayerIds.Count);

        if (room.PlayerIds.Count == 0)
        {
            _rooms.TryRemove(roomId, out _);
            Log.Information("[GameRoomManager] 游戏房间关闭 roomId={RoomId}", roomId);
        }

        return (ReturnCode.Success, roomId);
    }

    public void BroadcastToRoom(string roomId, NetPeer sender, ushort messageId, object data,
        DeliveryMethod method = DeliveryMethod.ReliableOrdered)
    {
        if (!_rooms.TryGetValue(roomId, out var room)) return;

        var frame = MessageHelper.CreateFrame(messageId, ReturnCode.Success, data);
        var peers = room.PlayerIds
            .Select(id => _playerManager.Get(id))
            .Where(p => p != null && p.Peer != sender)
            .Select(p => p!.Peer);
        MessageHelper.SendToAll(peers, frame, method);
    }

    public string? GetRoomId(NetPeer peer)
    {
        return _playerManager.GetByPeer(peer)?.CurrentRoomId;
    }
}
