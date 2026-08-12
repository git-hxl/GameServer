using System.Collections.Concurrent;
using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Player;

public class PlayerManager
{
    private readonly ConcurrentDictionary<long, LobbyPlayer> _players = new();
    private readonly ConcurrentDictionary<NetPeer, long> _peerToUserId = new();

    public int Count => _players.Count;

    public LobbyPlayer? Get(long userId)
    {
        _players.TryGetValue(userId, out var player);
        return player;
    }

    public LobbyPlayer? GetByPeer(NetPeer peer)
    {
        return _peerToUserId.TryGetValue(peer, out var userId) ? Get(userId) : null;
    }

    public long GetUserId(NetPeer peer)
    {
        _peerToUserId.TryGetValue(peer, out var userId);
        return userId;
    }

    public IEnumerable<LobbyPlayer> All => _players.Values;

    public LobbyPlayer Register(NetPeer peer, PlayerInfo info)
    {
        var player = new LobbyPlayer
        {
            Info = info,
            Peer = peer,
            State = PlayerState.InLobby
        };

        _players[info.UserId] = player;
        _peerToUserId[peer] = info.UserId;

        return player;
    }

    public bool Unregister(long userId)
    {
        if (_players.TryRemove(userId, out var player))
        {
            _peerToUserId.TryRemove(player.Peer, out _);
            return true;
        }
        return false;
    }

    public void RemoveByPeer(NetPeer peer)
    {
        if (_peerToUserId.TryRemove(peer, out var userId))
            _players.TryRemove(userId, out _);
    }

    public void SetState(long userId, PlayerState state, string? roomId = null)
    {
        if (_players.TryGetValue(userId, out var player))
        {
            player.State = state;
            player.CurrentRoomId = roomId ?? (state == PlayerState.InLobby ? null : player.CurrentRoomId);
        }
    }

    public PlayerState GetState(long userId)
    {
        return _players.TryGetValue(userId, out var player) ? player.State : PlayerState.InLobby;
    }

    public bool IsOnline(long userId)
    {
        return _players.TryGetValue(userId, out var player) && player.Peer.ConnectionState == ConnectionState.Connected;
    }
}
