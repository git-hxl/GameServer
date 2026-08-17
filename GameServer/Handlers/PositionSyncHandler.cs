using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using GameServer.Room;

namespace GameServer.Handlers;

public class PositionSyncHandler : MessageHandler<PositionSyncData>
{
    public override ushort MessageId => MessageIds.PositionSync;

    private readonly GameRoomManager _roomManager;

    public PositionSyncHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public override void HandleMessage(NetPeer peer, PositionSyncData data)
    {
        var roomId = _roomManager.GetRoomId(peer);
        if (roomId != null)
            _roomManager.BroadcastToRoom(roomId, peer, MessageId, data, DeliveryMethod.Sequenced);
    }
}
