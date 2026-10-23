using LiteNetLib;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using LobbyServer.Cluster;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class GameServerRegisterHandler : MessageHandler<GameServerInfo>
{
    public override ushort MessageId => MessageIds.GameServerRegister;

    private readonly GameServerRegistry _registry;
    private readonly RoomManager _rooms;

    public GameServerRegisterHandler(GameServerRegistry registry, RoomManager rooms)
    {
        _registry = registry;
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, GameServerInfo request)
    {
        _registry.Register(peer, request);
        Log.Information("[LobbyServer] GameServer 注册成功 address={Address} port={Port}", request.Address, request.Port);

        _rooms.ResyncGameServer(peer, request);
    }
}
