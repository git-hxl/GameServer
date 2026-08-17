using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using GameServer.Room;

namespace GameServer.Handlers;

public class ObjectDespawnHandler : MessageHandler<ObjectDespawnData>
{
    public override ushort MessageId => MessageIds.ObjectDespawn;

    private readonly GameRoomManager _roomManager;

    public ObjectDespawnHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public override void HandleMessage(NetPeer peer, ObjectDespawnData data)
    {
        var roomId = _roomManager.GetRoomId(peer);
        if (roomId != null)
            _roomManager.BroadcastToRoom(roomId, peer, MessageId, data, DeliveryMethod.Sequenced);
    }
}
