using LiteNetLib;
using MessagePack;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using GameServer.Room;

namespace GameServer.Handlers;

public class CreateGameRoomHandler : IGameHandler
{
    public ushort MessageId => MessageIds.CreateGameRoom;

    private readonly GameRoomManager _roomManager;

    public CreateGameRoomHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var req = MessagePackSerializer.Deserialize<CreateGameRoomRequest>(payload);
        if (req == null)
        {
            Log.Warning("[GameServer] CreateGameRoom 反序列化失败");
            return;
        }

        _roomManager.CreateRoom(req);
    }
}
