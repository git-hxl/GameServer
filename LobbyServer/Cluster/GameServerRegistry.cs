using System.Collections.Concurrent;
using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Cluster;

public class GameServerRegistry
{
    private readonly ConcurrentDictionary<int, ServerEntry> _byPort = new();
    private readonly ConcurrentDictionary<NetPeer, int> _portByPeer = new();

    public void Register(NetPeer peer, GameServerInfo info)
    {
        info.Address = ResolveAddress(peer);

        if (_portByPeer.TryGetValue(peer, out var oldPort) && oldPort != info.Port)
            _byPort.TryRemove(oldPort, out _);

        _portByPeer[peer] = info.Port;
        _byPort[info.Port] = new ServerEntry(peer, info);
    }

    public bool Update(NetPeer peer, GameServerInfo info)
    {
        if (!_portByPeer.TryGetValue(peer, out var port))
            return false;
        if (!_byPort.TryGetValue(port, out var entry) || entry.Peer != peer)
            return false;

        _byPort[port] = new ServerEntry(peer, new GameServerInfo
        {
            Address = entry.Info.Address,
            Port = entry.Info.Port,
            PlayerCount = info.PlayerCount,
            RoomCount = info.RoomCount,
            CpuPercent = info.CpuPercent,
            MemoryMB = info.MemoryMB
        });
        return true;
    }

    public void Remove(NetPeer peer)
    {
        if (_portByPeer.TryRemove(peer, out var port))
            _byPort.TryRemove(port, out _);
    }

    public GameServerInfo? Get(NetPeer peer)
    {
        if (!_portByPeer.TryGetValue(peer, out var port))
            return null;
        return _byPort.TryGetValue(port, out var entry) ? entry.Info : null;
    }

    public ServerEntry? GetByPort(int port)
    {
        if (_byPort.TryGetValue(port, out var entry) && entry.Peer.ConnectionState == ConnectionState.Connected)
            return entry;
        return null;
    }

    public KeyValuePair<NetPeer, GameServerInfo>? PickLeastLoaded()
    {
        var connected = _byPort
            .Where(s => s.Value.Peer.ConnectionState == ConnectionState.Connected)
            .ToList();

        if (connected.Count == 0)
            return null;

        var best = connected.MinBy(s => s.Value.Info.PlayerCount);
        return new KeyValuePair<NetPeer, GameServerInfo>(best.Value.Peer, best.Value.Info);
    }

    private static string ResolveAddress(NetPeer peer)
    {
        var ep = peer.Address;
        var epStr = ep.ToString();
        var colon = epStr.LastIndexOf(':');
        return colon > 0 ? epStr[..colon] : epStr;
    }

    public record ServerEntry(NetPeer Peer, GameServerInfo Info);
}
