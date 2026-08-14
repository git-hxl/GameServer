using System.Collections.Concurrent;
using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Cluster;

public class GameServerRegistry
{
    private readonly ConcurrentDictionary<NetPeer, GameServerInfo> _servers = new();

    public void Register(NetPeer peer, GameServerInfo info)
    {
        info.Address = ResolveAddress(peer);
        _servers[peer] = info;
    }

    public bool Update(NetPeer peer, GameServerInfo info)
    {
        if (!_servers.TryGetValue(peer, out var existing))
            return false;

        _servers[peer] = new GameServerInfo
        {
            Address = existing.Address,
            Port = existing.Port,
            PlayerCount = info.PlayerCount,
            RoomCount = info.RoomCount,
            CpuPercent = info.CpuPercent,
            MemoryMB = info.MemoryMB
        };
        return true;
    }

    public void Remove(NetPeer peer)
    {
        _servers.TryRemove(peer, out _);
    }

    public GameServerInfo? Get(NetPeer peer)
    {
        _servers.TryGetValue(peer, out var info);
        return info;
    }

    public KeyValuePair<NetPeer, GameServerInfo>? PickLeastLoaded()
    {
        var connected = _servers
            .Where(s => s.Key.ConnectionState == ConnectionState.Connected)
            .ToList();

        if (connected.Count == 0)
            return null;

        return connected.MinBy(s => s.Value.PlayerCount);
    }

    private static string ResolveAddress(NetPeer peer)
    {
        var ep = peer.Address;
        var epStr = ep.ToString();
        var colon = epStr.LastIndexOf(':');
        return colon > 0 ? epStr[..colon] : epStr;
    }
}
