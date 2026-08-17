using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using GameServer.Room;

namespace GameServer.Handlers;

public class CreateGameRoomHandler : MessageHandler<CreateGameRoomRequest>
{
    public override ushort MessageId => MessageIds.CreateGameRoom;

    private readonly GameRoomManager _roomManager;

    public CreateGameRoomHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public override void HandleMessage(NetPeer peer, CreateGameRoomRequest request)
    {
        _roomManager.CreateRoom(request);
    }
}
