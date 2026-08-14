using LiteNetLib;
using MessagePack;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using LobbyServer.Cluster;

namespace LobbyServer.Handlers;

public class GameServerUpdateHandler : ILobbyHandler
{
    public ushort MessageId => MessageIds.GameServerUpdate;
    public bool RequireAuth => false;

    private readonly GameServerRegistry _registry;

    public GameServerUpdateHandler(GameServerRegistry registry)
    {
        _registry = registry;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var req = MessagePackSerializer.Deserialize<GameServerInfo>(payload);
        if (req == null)
        {
            Log.Warning("[LobbyServer] GameServerUpdate 反序列化失败");
            return;
        }

        if (!_registry.Update(peer, req))
        {
            Log.Warning("[LobbyServer] 收到未注册 GameServer 的更新");
        }
    }
}
