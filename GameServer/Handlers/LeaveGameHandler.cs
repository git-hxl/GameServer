using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using GameServer.Room;

namespace GameServer.Handlers;

public class LeaveGameHandler : MessageHandler<LeaveGameRequest>
{
    public override ushort MessageId => MessageIds.LeaveGame;

    private readonly GameRoomManager _roomManager;

    public LeaveGameHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public override void HandleMessage(NetPeer peer, LeaveGameRequest request)
    {
        var (code, roomId) = _roomManager.LeaveGame(peer);
        MessageHelper.Send(peer, MessageId, code, new LeaveGameResponse { RoomId = roomId ?? "" });
    }
}
