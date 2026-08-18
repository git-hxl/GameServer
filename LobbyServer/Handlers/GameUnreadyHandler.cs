using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class GameUnreadyHandler : LobbyStateHandler<GameReadyRequest>
{
    public override ushort MessageId => MessageIds.GameUnready;

    private readonly RoomManager _rooms;

    public GameUnreadyHandler(PlayerManager players, RoomManager rooms) : base(players)
    {
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, GameReadyRequest request)
    {
        var userId = Players.GetUserId(peer);
        var (res, code) = _rooms.SetUnready(userId);
        MessageHelper.Send(peer, MessageId, code, res);
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new GameUnreadyResponse());
    }
}
