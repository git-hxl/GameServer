using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class JoinRoomHandler : LobbyStateHandler<JoinRoomRequest>
{
    public override ushort MessageId => MessageIds.JoinRoom;

    private readonly RoomManager _rooms;

    public JoinRoomHandler(PlayerManager players, RoomManager rooms) : base(players)
    {
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, JoinRoomRequest request)
    {
        var userId = Players.GetUserId(peer);
        var (res, code) = _rooms.JoinRoom(userId, request);
        MessageHelper.Send(peer, MessageId, code, res);
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new JoinRoomResponse());
    }
}
