using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using GameServer.Room;

namespace GameServer.Handlers;

public class AuthorizeGamePlayerHandler : MessageHandler<AuthorizeGamePlayerRequest>
{
    public override ushort MessageId => MessageIds.AuthorizeGamePlayer;

    private readonly GameRoomManager _roomManager;

    public AuthorizeGamePlayerHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public override void HandleMessage(NetPeer peer, AuthorizeGamePlayerRequest request)
    {
        _roomManager.AuthorizePlayer(request);
    }
}
