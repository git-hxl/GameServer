using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using GameServer.Room;

namespace GameServer.Handlers;

public class ObjectSpawnHandler : MessageHandler<ObjectSpawnData>
{
    public override ushort MessageId => MessageIds.ObjectSpawn;

    private readonly GameRoomManager _roomManager;

    public ObjectSpawnHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public override void HandleMessage(NetPeer peer, ObjectSpawnData data)
    {
        var roomId = _roomManager.GetRoomId(peer);
        if (roomId != null)
            _roomManager.BroadcastToRoom(roomId, peer, MessageId, data, DeliveryMethod.Sequenced);
    }
}
