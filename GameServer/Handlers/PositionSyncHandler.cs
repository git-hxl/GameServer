using LiteNetLib;
using MessagePack;
using SharedLib.Models;
using SharedLib.Protocol;
using GameServer.Room;

namespace GameServer.Handlers;

public class PositionSyncHandler : IGameHandler
{
    public ushort MessageId => MessageIds.PositionSync;

    private readonly GameRoomManager _roomManager;

    public PositionSyncHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var data = MessagePackSerializer.Deserialize<PositionSyncData>(payload);
        if (data == null) return;

        var roomId = _roomManager.GetRoomId(peer);
        if (roomId != null)
            _roomManager.BroadcastToRoom(roomId, peer, MessageId, data);
    }
}
