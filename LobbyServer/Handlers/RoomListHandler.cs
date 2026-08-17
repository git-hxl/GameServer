using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class RoomListHandler : MessageHandler<RoomListRequest>
{
    public override ushort MessageId => MessageIds.RoomList;

    private readonly RoomManager _rooms;

    public RoomListHandler(RoomManager rooms)
    {
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, RoomListRequest request)
    {
        var res = _rooms.GetRoomList();
        MessageHelper.Send(peer, MessageId, ReturnCode.Success, res);
    }
}
