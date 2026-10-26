using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers.Client;

public class CreateRoomHandler : LobbyStateHandler<CreateRoomRequest>
{
    public override ushort MessageId => MessageIds.CreateRoom;

    private readonly RoomManager _rooms;
    private readonly PlayerManager _players;

    public CreateRoomHandler(PlayerManager players, RoomManager rooms) : base(players)
    {
        _players = players;
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, CreateRoomRequest request)
    {
        var player = _players.GetByPeer(peer);
        if (player == null)
            return;

        var userId = player.Info.UserId;
        var (res, code) = _rooms.CreateRoom(request, userId);
        MessageHelper.Send(peer, MessageId, code, res);
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new CreateRoomResponse());
    }
}