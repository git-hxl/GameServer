using LiteNetLib;
using MessagePack;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class JoinRoomHandler : ILobbyHandler
{
    public ushort MessageId => MessageIds.JoinRoom;
    public bool RequireAuth => true;

    private readonly PlayerManager _players;
    private readonly RoomManager _rooms;

    public JoinRoomHandler(PlayerManager players, RoomManager rooms)
    {
        _players = players;
        _rooms = rooms;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var userId = _players.GetUserId(peer);
        if (userId == 0)
        {
            Log.Warning("[LobbyServer] JoinRoom 未登录");
            MessageHelper.Send(peer, MessageId, ReturnCode.NotInLobby, new JoinRoomResponse());
            return;
        }

        var req = MessagePackSerializer.Deserialize<JoinRoomRequest>(payload);
        if (req == null)
        {
            MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new JoinRoomResponse());
            return;
        }

        var (res, code) = _rooms.JoinRoom(userId, req);
        MessageHelper.Send(peer, MessageId, code, res);
    }
}
