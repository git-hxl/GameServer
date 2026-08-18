using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class LeaveRoomHandler : LobbyStateHandler<LeaveRoomRequest>
{
    public override ushort MessageId => MessageIds.LeaveRoom;

    private readonly RoomManager _rooms;

    public LeaveRoomHandler(PlayerManager players, RoomManager rooms) : base(players)
    {
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, LeaveRoomRequest request)
    {
        var userId = Players.GetUserId(peer);
        var (res, code) = _rooms.LeaveRoom(userId);
        MessageHelper.Send(peer, MessageId, code, res);
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new LeaveRoomResponse());
    }
}
