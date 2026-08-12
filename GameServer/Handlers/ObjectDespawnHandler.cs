using LiteNetLib;
using MessagePack;
using SharedLib.Models;
using SharedLib.Protocol;
using GameServer.Room;

namespace GameServer.Handlers;

public class ObjectDespawnHandler : IGameHandler
{
    public ushort MessageId => MessageIds.ObjectDespawn;

    private readonly GameRoomManager _roomManager;

    public ObjectDespawnHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var data = MessagePackSerializer.Deserialize<ObjectDespawnData>(payload);
        if (data == null) return;

        var roomId = _roomManager.GetRoomId(peer);
        if (roomId != null)
            _roomManager.BroadcastToRoom(roomId, peer, MessageId, data);
    }
}
