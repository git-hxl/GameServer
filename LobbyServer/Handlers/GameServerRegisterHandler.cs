using System.Collections.Concurrent;
using LiteNetLib;
using MessagePack;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;

namespace LobbyServer.Handlers;

public class GameServerRegisterHandler : ILobbyHandler
{
    public ushort MessageId => MessageIds.GameServerRegister;
    public bool RequireAuth => false;

    private readonly ConcurrentDictionary<NetPeer, GameServerInfo> _gameServers;

    public GameServerRegisterHandler(ConcurrentDictionary<NetPeer, GameServerInfo> gameServers)
    {
        _gameServers = gameServers;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var req = MessagePackSerializer.Deserialize<GameServerInfo>(payload);
        if (req == null)
        {
            Log.Warning("[LobbyServer] GameServerRegister 反序列化失败");
            return;
        }

        var ep = peer.Address;
        var epStr = ep.ToString();
        var colon = epStr.LastIndexOf(':');
        req.Address = colon > 0 ? epStr[..colon] : epStr;
        _gameServers[peer] = req;
        Log.Information("[LobbyServer] GameServer 注册成功 port={Port}", req.Port);
    }
}
