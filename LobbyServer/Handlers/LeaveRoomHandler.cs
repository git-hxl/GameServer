using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class LeaveRoomHandler : MessageHandler<LeaveRoomRequest>
{
    public override ushort MessageId => MessageIds.LeaveRoom;

    private readonly PlayerManager _players;
    private readonly RoomManager _rooms;

    public LeaveRoomHandler(PlayerManager players, RoomManager rooms)
    {
        _players = players;
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, LeaveRoomRequest request)
    {
        var userId = _players.GetUserId(peer);
        if (userId == 0)
        {
            MessageHelper.Send(peer, MessageId, ReturnCode.NotInRoom, new LeaveRoomResponse());
            return;
        }

        var (res, code) = _rooms.LeaveRoom(userId);
        MessageHelper.Send(peer, MessageId, code, res);
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new LeaveRoomResponse());
    }
}
