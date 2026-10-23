using System.Collections.Concurrent;
using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Player;

public class PlayerManager
{
    private readonly ConcurrentDictionary<long, LobbyPlayer> _players = new();

    public int Count => _players.Count;

    public int OnlineCount => _players.Values.Count(p => p.Peer is { ConnectionState: ConnectionState.Connected });

    public IEnumerable<LobbyPlayer> All => _players.Values;

    public IEnumerable<NetPeer> OnlinePeers
    {
        get
        {
            return _players.Values
                .Where(p => p.Peer is { ConnectionState: ConnectionState.Connected })
                .Select(p => p.Peer!);
        }
    }

    public LobbyPlayer? Get(long userId)
    {
        _players.TryGetValue(userId, out var player);
        return player;
    }

    public LobbyPlayer? GetByPeer(NetPeer peer)
    {
        return _players.Values.FirstOrDefault(p => ReferenceEquals(p.Peer, peer));
    }

    public long GetUserId(NetPeer peer)
    {
        return GetByPeer(peer)?.Info.UserId ?? 0;
    }

    public LobbyPlayer Add(NetPeer peer, PlayerInfo info)
    {
        var player = new LobbyPlayer
        {
            Info = info,
            Peer = peer
        };

        _players[info.UserId] = player;
        return player;
    }

    public LobbyPlayer AddOffline(PlayerInfo info, string roomId)
    {
        var player = new LobbyPlayer
        {
            Info = info,
            Peer = null,
            CurrentRoomId = roomId
        };

        _players[info.UserId] = player;
        return player;
    }

    public void BindOnline(LobbyPlayer player, NetPeer peer, PlayerInfo info)
    {
        player.Peer = peer;
        player.Info = info;
    }

    public void UnbindPeer(NetPeer peer)
    {
        var player = GetByPeer(peer);
        if (player == null)
            return;

        player.Peer = null;
    }

    public bool Remove(long userId)
    {
        return _players.TryRemove(userId, out _);
    }

    public void SetRoom(long userId, string roomId)
    {
        if (_players.TryGetValue(userId, out var player))
            player.CurrentRoomId = roomId;
    }

    public void ClearRoom(long userId)
    {
        if (_players.TryGetValue(userId, out var player))
            player.CurrentRoomId = null;
    }

    public bool IsOnline(long userId)
    {
        return _players.TryGetValue(userId, out var player) &&
               player.Peer is { ConnectionState: ConnectionState.Connected };
    }
}
