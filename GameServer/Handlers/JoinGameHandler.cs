using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using GameServer.Room;

namespace GameServer.Handlers;

public class JoinGameHandler : MessageHandler<JoinGameRequest>
{
    public override ushort MessageId => MessageIds.JoinGame;

    private readonly GameRoomManager _roomManager;

    public JoinGameHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public override void HandleMessage(NetPeer peer, JoinGameRequest request)
    {
        var (res, code) = _roomManager.JoinGame(peer, request);
        MessageHelper.Send(peer, MessageId, code, res);
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new JoinGameResponse());
    }
}
