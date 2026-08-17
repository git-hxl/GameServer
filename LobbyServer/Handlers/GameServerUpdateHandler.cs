using LiteNetLib;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using LobbyServer.Cluster;

namespace LobbyServer.Handlers;

public class GameServerUpdateHandler : MessageHandler<GameServerInfo>
{
    public override ushort MessageId => MessageIds.GameServerUpdate;

    private readonly GameServerRegistry _registry;

    public GameServerUpdateHandler(GameServerRegistry registry)
    {
        _registry = registry;
    }

    public override void HandleMessage(NetPeer peer, GameServerInfo request)
    {
        if (!_registry.Update(peer, request))
        {
            Log.Warning("[LobbyServer] 收到未注册 GameServer 的更新");
        }
    }
}
