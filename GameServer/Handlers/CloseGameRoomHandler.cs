using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using GameServer.Room;

namespace GameServer.Handlers;

public class CloseGameRoomHandler : MessageHandler<CloseGameRoomRequest>
{
    public override ushort MessageId => MessageIds.CloseGameRoom;

    private readonly GameRoomManager _roomManager;

    public CloseGameRoomHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public override void HandleMessage(NetPeer peer, CloseGameRoomRequest request)
    {
        _roomManager.CloseRoom(request);
    }
}
