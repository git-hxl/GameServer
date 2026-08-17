using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class GameStartHandler : MessageHandler<GameStartRequest>
{
    public override ushort MessageId => MessageIds.GameStart;

    private readonly PlayerManager _players;
    private readonly RoomManager _rooms;

    public GameStartHandler(PlayerManager players, RoomManager rooms)
    {
        _players = players;
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, GameStartRequest request)
    {
        var userId = _players.GetUserId(peer);
        if (userId == 0)
        {
            MessageHelper.Send(peer, MessageId, ReturnCode.NotInRoom, new GameStartResponse { Code = (int)ReturnCode.NotInRoom });
            return;
        }

        var (code, _) = _rooms.StartGame(userId);
        MessageHelper.Send(peer, MessageId, code, new GameStartResponse { Code = (int)code });
    }
}
