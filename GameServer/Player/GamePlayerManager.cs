using System.Collections.Concurrent;
using LiteNetLib;
using SharedLib.Models;

namespace GameServer.Player;

public class GamePlayerManager
{
    private readonly ConcurrentDictionary<long, GamePlayer> _players = new();
    private readonly ConcurrentDictionary<NetPeer, long> _peerToUserId = new();

    public int Count => _players.Count;

    public GamePlayer? Get(long userId)
    {
        _players.TryGetValue(userId, out var player);
        return player;
    }

    public GamePlayer? GetByPeer(NetPeer peer)
    {
        return _peerToUserId.TryGetValue(peer, out var userId) ? Get(userId) : null;
    }

    public GamePlayer Add(NetPeer peer, PlayerInfo info)
    {
        var player = new GamePlayer
        {
            Info = info,
            Peer = peer
        };

        _players[info.UserId] = player;
        _peerToUserId[peer] = info.UserId;

        return player;
    }

    public bool Remove(long userId)
    {
        if (_players.TryRemove(userId, out var player))
        {
            _peerToUserId.TryRemove(player.Peer, out _);
            return true;
        }

        return false;
    }
}
