using LiteNetLib;
using MessagePack;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using LobbyServer.Cluster;

namespace LobbyServer.Handlers;

public class GameServerRegisterHandler : ILobbyHandler
{
    public ushort MessageId => MessageIds.GameServerRegister;
    public bool RequireAuth => false;

    private readonly GameServerRegistry _registry;

    public GameServerRegisterHandler(GameServerRegistry registry)
    {
        _registry = registry;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var req = MessagePackSerializer.Deserialize<GameServerInfo>(payload);
        if (req == null)
        {
            Log.Warning("[LobbyServer] GameServerRegister 反序列化失败");
            return;
        }

        _registry.Register(peer, req);
        Log.Information("[LobbyServer] GameServer 注册成功 port={Port}", req.Port);
    }
}
