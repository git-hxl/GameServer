using LiteNetLib;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using LobbyServer.Cluster;

namespace LobbyServer.Handlers;

public class GameServerRegisterHandler : MessageHandler<GameServerInfo>
{
    public override ushort MessageId => MessageIds.GameServerRegister;

    private readonly GameServerRegistry _registry;

    public GameServerRegisterHandler(GameServerRegistry registry)
    {
        _registry = registry;
    }

    public override void HandleMessage(NetPeer peer, GameServerInfo request)
    {
        _registry.Register(peer, request);
        Log.Information("[LobbyServer] GameServer 注册成功 port={Port}", request.Port);
    }
}
