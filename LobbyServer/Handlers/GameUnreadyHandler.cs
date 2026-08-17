using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class GameUnreadyHandler : MessageHandler<GameReadyRequest>
{
    public override ushort MessageId => MessageIds.GameUnready;

    private readonly PlayerManager _players;
    private readonly RoomManager _rooms;

    public GameUnreadyHandler(PlayerManager players, RoomManager rooms)
    {
        _players = players;
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, GameReadyRequest request)
    {
        var userId = _players.GetUserId(peer);
        if (userId == 0)
        {
            MessageHelper.Send(peer, MessageId, ReturnCode.NotInRoom, new GameUnreadyResponse());
            return;
        }

        var (res, code) = _rooms.SetUnready(userId);
        MessageHelper.Send(peer, MessageId, code, res);
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new GameUnreadyResponse());
    }
}
