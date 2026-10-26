using System.Collections.Concurrent;
using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Player;

/// <summary>
/// 大厅玩家与当前连接的管理器：userId → LobbyPlayer，PeerId → NetPeer。
/// </summary>
public class PlayerManager
{
    private readonly ConcurrentDictionary<long, LobbyPlayer> _players = new();

    /// <summary>
    /// 玩家总数，包含离线玩家。
    /// </summary>
    public int Count => _players.Count;

    /// <summary>
    /// 当前在线玩家数。
    /// </summary>
    public int OnlineCount => _players.Values.Count((a) => a.Peer != null);

    /// <summary>
    /// 全部玩家，包含离线玩家。
    /// </summary>
    public IEnumerable<LobbyPlayer> All => _players.Values;

    public IEnumerable<NetPeer> OnlinePeers => _players.Values.Where((a) => a.Peer != null).Select(a => a.Peer!);

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
        return _players.Values.FirstOrDefault((a) => a.Peer != null && a.Peer.Equals(peer));
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
            Info = info,
        };
        
        player.SetPeer(peer);
        _players[info.UserId] = player;
        return player;
    }

    /// <summary>
    /// 从大厅移除玩家，并重置 PeerId。
    /// </summary>
    /// <param name="userId">玩家 ID。</param>
    /// <returns>玩家存在并被移除返回 true。</returns>
    public bool Remove(long userId)
    {
        return _players.TryRemove(userId, out _);
    }
}