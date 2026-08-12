using LiteNetLib;
using MessagePack;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class CreateRoomHandler : ILobbyHandler
{
    public ushort MessageId => MessageIds.CreateRoom;
    public bool RequireAuth => true;

    private readonly PlayerManager _players;
    private readonly RoomManager _rooms;

    public CreateRoomHandler(PlayerManager players, RoomManager rooms)
    {
        _players = players;
        _rooms = rooms;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var userId = _players.GetUserId(peer);
        if (userId == 0)
        {
            Log.Warning("[LobbyServer] CreateRoom 未登录");
            MessageHelper.Send(peer, MessageId, ReturnCode.NotInLobby, new CreateRoomResponse());
            return;
        }

        var req = MessagePackSerializer.Deserialize<CreateRoomRequest>(payload);
        if (req == null)
        {
            MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new CreateRoomResponse());
            return;
        }

        var (res, code) = _rooms.CreateRoom(userId, req);
        MessageHelper.Send(peer, MessageId, code, res);
    }
}
