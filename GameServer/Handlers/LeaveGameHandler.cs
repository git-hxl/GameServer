using LiteNetLib;
using SharedLib.Protocol;
using SharedLib.Utils;
using GameServer.Room;

namespace GameServer.Handlers;

public class LeaveGameHandler : IGameHandler
{
    public ushort MessageId => MessageIds.LeaveGame;

    private readonly GameRoomManager _roomManager;

    public LeaveGameHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var (code, roomId) = _roomManager.LeaveGame(peer);
        MessageHelper.Send(peer, MessageId, code, new { RoomId = roomId ?? "" });
    }
}
