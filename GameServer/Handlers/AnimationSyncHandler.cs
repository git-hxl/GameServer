using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using GameServer.Room;

namespace GameServer.Handlers;

public class AnimationSyncHandler : MessageHandler<AnimationSyncData>
{
    public override ushort MessageId => MessageIds.AnimationSync;

    private readonly GameRoomManager _roomManager;

    public AnimationSyncHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public override void HandleMessage(NetPeer peer, AnimationSyncData data)
    {
        var roomId = _roomManager.GetRoomId(peer);
        if (roomId != null)
            _roomManager.BroadcastToRoom(roomId, peer, MessageId, data, DeliveryMethod.Sequenced);
    }
}
