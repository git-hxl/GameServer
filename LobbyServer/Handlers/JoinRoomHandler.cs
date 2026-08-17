using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class JoinRoomHandler : MessageHandler<JoinRoomRequest>
{
    public override ushort MessageId => MessageIds.JoinRoom;

    private readonly PlayerManager _players;
    private readonly RoomManager _rooms;

    public JoinRoomHandler(PlayerManager players, RoomManager rooms)
    {
        _players = players;
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, JoinRoomRequest request)
    {
        var userId = _players.GetUserId(peer);
        if (userId == 0)
        {
            MessageHelper.Send(peer, MessageId, ReturnCode.NotInLobby, new JoinRoomResponse());
            return;
        }

        var (res, code) = _rooms.JoinRoom(userId, request);
        MessageHelper.Send(peer, MessageId, code, res);
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new JoinRoomResponse());
    }
}
