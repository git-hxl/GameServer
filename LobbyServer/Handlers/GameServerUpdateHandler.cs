using System.Collections.Concurrent;
using LiteNetLib;
using MessagePack;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;

namespace LobbyServer.Handlers;

public class GameServerUpdateHandler : ILobbyHandler
{
    public ushort MessageId => MessageIds.GameServerUpdate;
    public bool RequireAuth => false;

    private readonly ConcurrentDictionary<NetPeer, GameServerInfo> _gameServers;

    public GameServerUpdateHandler(ConcurrentDictionary<NetPeer, GameServerInfo> gameServers)
    {
        _gameServers = gameServers;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var req = MessagePackSerializer.Deserialize<GameServerInfo>(payload);
        if (req == null)
        {
            Log.Warning("[LobbyServer] GameServerUpdate 反序列化失败");
            return;
        }

        if (!_gameServers.TryGetValue(peer, out var gsInfo))
        {
            Log.Warning("[LobbyServer] 收到未注册 GameServer 的更新");
            return;
        }

        gsInfo.PlayerCount = req.PlayerCount;
        gsInfo.RoomCount = req.RoomCount;
        gsInfo.CpuPercent = req.CpuPercent;
        gsInfo.MemoryMB = req.MemoryMB;
    }
}
