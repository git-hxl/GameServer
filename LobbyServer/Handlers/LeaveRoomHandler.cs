using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class LeaveRoomHandler : ILobbyHandler
{
    public ushort MessageId => MessageIds.LeaveRoom;
    public bool RequireAuth => true;

    private readonly PlayerManager _players;
    private readonly RoomManager _rooms;

    public LeaveRoomHandler(PlayerManager players, RoomManager rooms)
    {
        _players = players;
        _rooms = rooms;
    }

    public void Handle(NetPeer peer, byte[] payload)
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
}
