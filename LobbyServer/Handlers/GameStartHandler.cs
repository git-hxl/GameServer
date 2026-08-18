using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class GameStartHandler : LobbyStateHandler<GameStartRequest>
{
    public override ushort MessageId => MessageIds.GameStart;

    private readonly RoomManager _rooms;

    public GameStartHandler(PlayerManager players, RoomManager rooms) : base(players)
    {
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, GameStartRequest request)
    {
        var userId = Players.GetUserId(peer);
        var (code, _) = _rooms.StartGame(userId);
        MessageHelper.Send(peer, MessageId, code);
    }
}
