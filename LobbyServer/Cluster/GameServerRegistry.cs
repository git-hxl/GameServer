using System.Collections.Concurrent;
using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Cluster;

public class GameServerRegistry
{
    private readonly ConcurrentDictionary<string, ServerEntry> _byKey = new();
    private readonly ConcurrentDictionary<NetPeer, string> _keyByPeer = new();

    public void Register(NetPeer peer, GameServerInfo info)
    {
        var key = MakeKey(info.Address, info.Port);

        if (_keyByPeer.TryGetValue(peer, out var oldKey) && oldKey != key &&
            _byKey.TryGetValue(oldKey, out var oldEntry) && oldEntry.Peer == peer)
        {
            _byKey.TryRemove(oldKey, out _);
        }

        _keyByPeer[peer] = key;
        _byKey[key] = new ServerEntry(peer, info);
    }

    public bool Update(NetPeer peer, GameServerInfo info)
    {
        if (!_keyByPeer.TryGetValue(peer, out var key))
            return false;
        if (!_byKey.TryGetValue(key, out var entry) || entry.Peer != peer)
            return false;

        _byKey[key] = new ServerEntry(peer, new GameServerInfo
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
        if (_keyByPeer.TryRemove(peer, out var key) &&
            _byKey.TryGetValue(key, out var entry) && entry.Peer == peer)
        {
            _byKey.TryRemove(key, out _);
        }
    }

    public GameServerInfo? Get(NetPeer peer)
    {
        if (!_keyByPeer.TryGetValue(peer, out var key))
            return null;
        return _byKey.TryGetValue(key, out var entry) ? entry.Info : null;
    }

    public ServerEntry? GetByAddressPort(string address, int port)
    {
        var key = MakeKey(address, port);
        if (_byKey.TryGetValue(key, out var entry) && entry.Peer.ConnectionState == ConnectionState.Connected)
            return entry;
        return null;
    }

    public KeyValuePair<NetPeer, GameServerInfo>? PickLeastLoaded()
    {
        var connected = _byKey
            .Where(s => s.Value.Peer.ConnectionState == ConnectionState.Connected)
            .ToList();

        if (connected.Count == 0)
            return null;

        var best = connected.MinBy(s => s.Value.Info.PlayerCount);
        return new KeyValuePair<NetPeer, GameServerInfo>(best.Value.Peer, best.Value.Info);
    }

    private static string MakeKey(string address, int port) => $"{address}:{port}";
}
