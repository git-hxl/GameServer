using LiteNetLib;
using MessagePack;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using GameServer.Room;

namespace GameServer.Handlers;

public class JoinGameHandler : IGameHandler
{
    public ushort MessageId => MessageIds.JoinGame;

    private readonly GameRoomManager _roomManager;

    public JoinGameHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var req = MessagePackSerializer.Deserialize<JoinGameRequest>(payload);
        if (req == null)
        {
            Log.Warning("[GameServer] JoinGame 反序列化失败");
            MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new JoinGameResponse());
            return;
        }

        var (res, code) = _roomManager.JoinGame(peer, req);
        MessageHelper.Send(peer, MessageId, code, res);
    }
}
