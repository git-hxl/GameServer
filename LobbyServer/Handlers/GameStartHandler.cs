using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Player;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class GameStartHandler : ILobbyHandler
{
    public ushort MessageId => MessageIds.GameStart;
    public bool RequireAuth => true;

    private readonly PlayerManager _players;
    private readonly RoomManager _rooms;

    public GameStartHandler(PlayerManager players, RoomManager rooms)
    {
        _players = players;
        _rooms = rooms;
    }

    public void Handle(NetPeer peer, byte[] payload)
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
