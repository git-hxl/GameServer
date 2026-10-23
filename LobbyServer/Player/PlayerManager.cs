using System.Collections.Concurrent;
using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Player;

/// <summary>
/// 大厅玩家与当前连接的管理器：userId → LobbyPlayer，PeerId → NetPeer。
/// </summary>
public class PlayerManager
{
    private const int NoPeerId = -1;

    private readonly ConcurrentDictionary<long, LobbyPlayer> _players = new();
    private readonly ConcurrentDictionary<int, NetPeer> _peersById = new();
    private int _nextPeerId;

    /// <summary>
    /// 玩家总数，包含离线玩家。
    /// </summary>
    public int Count => _players.Count;

    /// <summary>
    /// 当前在线玩家数。
    /// </summary>
    public int OnlineCount => _peersById.Values.Count(peer => peer.ConnectionState == ConnectionState.Connected);

    /// <summary>
    /// 全部玩家，包含离线玩家。
    /// </summary>
    public IEnumerable<LobbyPlayer> All => _players.Values;

    /// <summary>
    /// 当前在线的连接。
    /// </summary>
    public IEnumerable<NetPeer> OnlinePeers =>
        _peersById.Values.Where(peer => peer.ConnectionState == ConnectionState.Connected);

    /// <summary>
    /// 按 userId 获取玩家。
    /// </summary>
    /// <param name="userId">玩家 ID。</param>
    /// <returns>玩家；不存在返回 null。</returns>
    public LobbyPlayer? Get(long userId)
    {
        _players.TryGetValue(userId, out var player);
        return player;
    }

    /// <summary>
    /// 按当前连接查找玩家。
    /// </summary>
    /// <param name="peer">客户端连接。</param>
    /// <returns>玩家；该连接未绑定玩家返回 null。</returns>
    public LobbyPlayer? GetByPeer(NetPeer peer)
    {
        if (peer.Tag is not LobbyPlayer player || player.PeerId == NoPeerId)
            return null;

        return _peersById.TryGetValue(player.PeerId, out var mapped) && ReferenceEquals(mapped, peer)
            ? player
            : null;
    }

    /// <summary>
    /// 按当前连接获取 userId。
    /// </summary>
    /// <param name="peer">客户端连接。</param>
    /// <returns>userId；未绑定返回 0。</returns>
    public long GetUserId(NetPeer peer)
    {
        return GetByPeer(peer)?.Info.UserId ?? 0;
    }

    /// <summary>
    /// 按 userId 获取当前连接。
    /// </summary>
    /// <param name="userId">玩家 ID。</param>
    /// <returns>在线连接；离线或不存在返回 null。</returns>
    public NetPeer? GetPeer(long userId)
    {
        if (!_players.TryGetValue(userId, out var player) || player.PeerId == NoPeerId)
            return null;

        return _peersById.TryGetValue(player.PeerId, out var peer) ? peer : null;
    }

    /// <summary>
    /// 按玩家获取当前连接。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <returns>在线连接；离线返回 null。</returns>
    public NetPeer? GetPeer(LobbyPlayer player)
    {
        return GetPeer(player.Info.UserId);
    }

    /// <summary>
    /// 新增在线玩家并绑定连接。
    /// </summary>
    /// <param name="peer">客户端连接。</param>
    /// <param name="info">玩家信息。</param>
    /// <returns>新建的玩家。</returns>
    public LobbyPlayer Add(NetPeer peer, PlayerInfo info)
    {
        var player = new LobbyPlayer
        {
            Info = info
        };

        Attach(peer, player);
        _players[info.UserId] = player;
        return player;
    }

    /// <summary>
    /// 新增离线玩家。
    /// </summary>
    /// <param name="info">玩家信息。</param>
    /// <param name="roomId">所在房间 ID。</param>
    /// <returns>新建的离线玩家。</returns>
    public LobbyPlayer AddOffline(PlayerInfo info, string roomId)
    {
        var player = new LobbyPlayer
        {
            Info = info,
            PeerId = NoPeerId,
            CurrentRoomId = roomId
        };

        _players[info.UserId] = player;
        return player;
    }

    /// <summary>
    /// 将已有玩家绑定到新连接，替换旧的 PeerId。
    /// </summary>
    /// <param name="player">已有玩家。</param>
    /// <param name="peer">新连接。</param>
    /// <param name="info">更新后的玩家信息。</param>
    public void BindOnline(LobbyPlayer player, NetPeer peer, PlayerInfo info)
    {
        Detach(player);
        player.Info = info;
        Attach(peer, player);
    }

    /// <summary>
    /// 解除连接绑定，玩家保留但 PeerId 重置为 -1。
    /// </summary>
    /// <param name="peer">断开的连接。</param>
    public void UnbindPeer(NetPeer peer)
    {
        var player = GetByPeer(peer);
        if (player != null)
            Detach(player);
    }

    /// <summary>
    /// 从大厅移除玩家，并重置 PeerId。
    /// </summary>
    /// <param name="userId">玩家 ID。</param>
    /// <returns>玩家存在并被移除返回 true。</returns>
    public bool Remove(long userId)
    {
        if (!_players.TryRemove(userId, out var player))
            return false;

        Detach(player);
        player.PeerId = NoPeerId;
        return true;
    }

    /// <summary>
    /// 设置玩家所在房间。
    /// </summary>
    /// <param name="userId">玩家 ID。</param>
    /// <param name="roomId">房间 ID。</param>
    public void SetRoom(long userId, string roomId)
    {
        if (_players.TryGetValue(userId, out var player))
            player.CurrentRoomId = roomId;
    }

    /// <summary>
    /// 清空玩家所在房间。
    /// </summary>
    /// <param name="userId">玩家 ID。</param>
    public void ClearRoom(long userId)
    {
        if (_players.TryGetValue(userId, out var player))
            player.CurrentRoomId = null;
    }

    /// <summary>
    /// 判断玩家当前是否在线。
    /// </summary>
    /// <param name="userId">玩家 ID。</param>
    /// <returns>在线返回 true。</returns>
    public bool IsOnline(long userId)
    {
        return GetPeer(userId) is { ConnectionState: ConnectionState.Connected };
    }

    /// <summary>
    /// 为玩家分配新的 PeerId 并绑定连接。
    /// </summary>
    /// <param name="peer">客户端连接。</param>
    /// <param name="player">玩家。</param>
    private void Attach(NetPeer peer, LobbyPlayer player)
    {
        // 如果该连接之前绑定过其他玩家，先解除旧绑定。
        if (peer.Tag is LobbyPlayer previous)
            Detach(previous);

        Detach(player);

        var peerId = Interlocked.Increment(ref _nextPeerId);
        player.PeerId = peerId;
        _peersById[peerId] = peer;
        peer.Tag = player;
    }

    /// <summary>
    /// 解除玩家与连接的绑定，并重置 PeerId。
    /// </summary>
    /// <param name="player">玩家。</param>
    private void Detach(LobbyPlayer player)
    {
        if (player.PeerId == NoPeerId)
            return;

        if (_peersById.TryRemove(player.PeerId, out var peer) && ReferenceEquals(peer.Tag, player))
            peer.Tag = null;

        player.PeerId = NoPeerId;
    }
}
