using LiteNetLib;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class RoomListHandler : ILobbyHandler
{
    public ushort MessageId => MessageIds.RoomList;
    public bool RequireAuth => false;

    private readonly RoomManager _rooms;

    public RoomListHandler(RoomManager rooms)
    {
        _rooms = rooms;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var res = _rooms.GetRoomList();
        MessageHelper.Send(peer, MessageId, ReturnCode.Success, res);
    }
}
