using System.Collections.Concurrent;
using LiteNetLib;
using LobbyServer.Persistence;
using SharedLib.Models;

namespace LobbyServer.Player;

/// <summary>
/// 大厅玩家管理器：内存中的 userId → LobbyPlayer，并负责玩家资料持久化。
/// </summary>
public class PlayerManager
{
    private readonly ConcurrentDictionary<long, LobbyPlayer> _players = new();
    private readonly ILobbyPlayerStore _playerStore;

    public PlayerManager(ILobbyPlayerStore playerStore)
    {
        _playerStore = playerStore;
    }

    /// <summary>
    /// 玩家总数，包含离线玩家。
    /// </summary>
    public int Count => _players.Count;

    /// <summary>
    /// 当前在线玩家数。
    /// </summary>
    public int OnlineCount => _players.Values.Count(a => a.Peer != null);

    /// <summary>
    /// 全部玩家，包含离线玩家。
    /// </summary>
    public IEnumerable<LobbyPlayer> All => _players.Values;

    /// <summary>
    /// 当前在线的连接。
    /// </summary>
    public IEnumerable<NetPeer> OnlinePeers => _players.Values.Where(a => a.Peer != null).Select(a => a.Peer!);

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
        return _players.Values.FirstOrDefault(a => a.Peer != null && a.Peer.Equals(peer));
    }

    /// <summary>
    /// 新增在线玩家并绑定连接，同时保存玩家资料。
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

        player.SetPeer(peer);
        _players[info.UserId] = player;
        _playerStore.Save(info);
        return player;
    }

    /// <summary>
    /// 恢复离线玩家（Peer 为空），不写回 Redis。
    /// </summary>
    /// <param name="info">玩家信息。</param>
    /// <returns>恢复的离线玩家。</returns>
    public LobbyPlayer AddOffline(PlayerInfo info)
    {
        var player = new LobbyPlayer
        {
            Info = info
        };

        _players[info.UserId] = player;
        return player;
    }

    /// <summary>
    /// 从玩家资料存储恢复全部玩家，全部为离线状态。
    /// </summary>
    public void RestoreFromStore()
    {
        var players = _playerStore.LoadAll();

        foreach (var info in players)
            AddOffline(info);

        Serilog.Log.Information("[PlayerManager] 从存储恢复玩家数={Count}", _players.Count);
    }

    /// <summary>
    /// 将已有离线玩家绑定到新连接，并更新玩家资料。
    /// </summary>
    /// <param name="player">已有玩家。</param>
    /// <param name="peer">新连接。</param>
    /// <param name="info">最新玩家信息。</param>
    public void BindOnline(LobbyPlayer player, NetPeer peer, PlayerInfo info)
    {
        player.Info = info;
        player.SetPeer(peer);
        _playerStore.Save(info);
    }

    /// <summary>
    /// 从大厅移除玩家，并删除玩家资料。
    /// </summary>
    /// <param name="userId">玩家 ID。</param>
    /// <returns>玩家存在并被移除返回 true。</returns>
    public bool Remove(long userId)
    {
        if (!_players.TryRemove(userId, out _))
            return false;

        _playerStore.Delete(userId);
        return true;
    }
}
