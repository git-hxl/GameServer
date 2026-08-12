using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;

namespace GameServer.Room;

public class GameRoomManager
{
    private readonly NetManager _netManager;
    private readonly Dictionary<string, GameRoom> _rooms = new();

    public int PlayerCount => _rooms.Values.Sum(r => r.Players.Count);
    public int RoomCount => _rooms.Count;

    public GameRoomManager(NetManager netManager)
    {
        _netManager = netManager;
    }

    public void CreateRoom(CreateGameRoomRequest request)
    {
        var room = new GameRoom
        {
            RoomId = request.RoomId,
            RoomType = request.RoomType,
            OwnerUserId = request.OwnerUserId
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

        if (room.Players.ContainsKey(peer))
        {
            Log.Warning("[GameRoomManager] 加入游戏失败：已在房间中 userId={UserId} roomId={RoomId}",
                request.Player.UserId, request.RoomId);
            return (new JoinGameResponse(), ReturnCode.AlreadyInRoom);
        }

        var player = request.Player;
        room.Players[peer] = player;

        // 通知其他人
        var notify = new JoinGameNotify { RoomId = request.RoomId, Player = player };
        foreach (var otherPeer in room.Players.Keys.Where(p => p != peer))
        {
            Send(otherPeer, MessageIds.JoinGameNotify, ReturnCode.Success, notify);
        }

        Log.Information("[GameRoomManager] 玩家加入游戏房间 roomId={RoomId} userId={UserId} 当前人数={Count}",
            request.RoomId, player.UserId, room.Players.Count);

        return (new JoinGameResponse
        {
            RoomId = request.RoomId,
            RoomType = room.RoomType,
            OwnerUserId = room.OwnerUserId,
            Players = room.Players.Values.ToList()
        }, ReturnCode.Success);
    }

    public (ReturnCode Code, string? RoomId) LeaveGame(NetPeer peer)
    {
        foreach (var (roomId, room) in _rooms)
        {
            if (room.Players.TryGetValue(peer, out var player) && room.Players.Remove(peer))
            {
                Log.Information("[GameRoomManager] 玩家离开游戏房间 roomId={RoomId} 剩余人数={Count}",
                    roomId, room.Players.Count);

                var notify = new LeaveGameNotify { UserId = player.UserId };
                foreach (var otherPeer in room.Players.Keys)
                {
                    Send(otherPeer, MessageIds.LeaveGameNotify, ReturnCode.Success, notify);
                }

                if (room.Players.Count == 0)
                {
                    _rooms.Remove(roomId);
                    Log.Information("[GameRoomManager] 游戏房间关闭 roomId={RoomId}", roomId);
                }
                return (ReturnCode.Success, roomId);
            }
        }
        return (ReturnCode.NotInRoom, null);
    }

    public void RemovePlayer(NetPeer peer)
    {
        foreach (var (roomId, room) in _rooms)
        {
            if (room.Players.TryGetValue(peer, out var player) && room.Players.Remove(peer))
            {
                Log.Information("[GameRoomManager] 玩家断线离开游戏房间 roomId={RoomId} 剩余人数={Count}",
                    roomId, room.Players.Count);

                var notify = new LeaveGameNotify { UserId = player.UserId };
                foreach (var otherPeer in room.Players.Keys)
                {
                    Send(otherPeer, MessageIds.LeaveGameNotify, ReturnCode.Success, notify);
                }

                if (room.Players.Count == 0)
                {
                    _rooms.Remove(roomId);
                    Log.Information("[GameRoomManager] 游戏房间关闭 roomId={RoomId}", roomId);
                }
            }
        }
    }

    public void BroadcastToRoom(string roomId, NetPeer sender, ushort messageId, object data)
    {
        if (!_rooms.TryGetValue(roomId, out var room)) return;

        foreach (var otherPeer in room.Players.Keys.Where(p => p != sender))
        {
            Send(otherPeer, messageId, ReturnCode.Success, data);
        }
    }

    public string? GetRoomId(NetPeer peer)
    {
        foreach (var (roomId, room) in _rooms)
        {
            if (room.Players.ContainsKey(peer))
                return roomId;
        }
        return null;
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
