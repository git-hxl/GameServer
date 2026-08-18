using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class CreateRoomHandler : LobbyStateHandler<CreateRoomRequest>
{
    public override ushort MessageId => MessageIds.CreateRoom;

    private readonly RoomManager _rooms;

    public CreateRoomHandler(PlayerManager players, RoomManager rooms) : base(players)
    {
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, CreateRoomRequest request)
    {
        var userId = Players.GetUserId(peer);
        var (res, code) = _rooms.CreateRoom(userId, request);
        MessageHelper.Send(peer, MessageId, code, res);
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new CreateRoomResponse());
    }
}
