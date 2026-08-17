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

    public void CreateRoom(CreateGameRoomRequest request)
    {
        var room = new GameRoom
        {
            RoomId = request.RoomId,
            RoomType = request.RoomType,
            OwnerUserId = request.OwnerUserId,
            IsStarted = true
        };

        _rooms[request.RoomId] = room;

        Log.Information("[GameRoomManager] 游戏房间创建 roomId={RoomId} type={RoomType} owner={OwnerUserId}",
            request.RoomId, request.RoomType, request.OwnerUserId);
    }

    public (JoinGameResponse Response, ReturnCode Code) JoinGame(NetPeer peer, JoinGameRequest request)
    {
        if (!_rooms.TryGetValue(request.RoomId, out var room))
        {
            Log.Warning("[GameRoomManager] 加入游戏失败：房间未找到 roomId={RoomId}", request.RoomId);
            return (new JoinGameResponse(), ReturnCode.RoomNotFound);
        }

        var userId = request.Player.UserId;
        if (room.PlayerIds.Contains(userId))
        {
            Log.Warning("[GameRoomManager] 加入游戏失败：已在房间中 userId={UserId} roomId={RoomId}",
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

    public void BroadcastToRoom(string roomId, NetPeer sender, ushort messageId, object data, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
    {
        if (!_rooms.TryGetValue(roomId, out var room)) return;

        var frame = MessageHelper.SerializeFrame(messageId, ReturnCode.Success, data);
        foreach (var id in room.PlayerIds)
        {
            var p = _playerManager.Get(id);
            if (p != null && p.Peer != sender)
                MessageHelper.Send(p.Peer, frame, method);
        }
    }

    public string? GetRoomId(NetPeer peer)
    {
        return _playerManager.GetByPeer(peer)?.CurrentRoomId;
    }

}
