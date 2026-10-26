using Serilog;
using SharedLib.Models;
using SharedLib.Redis;

namespace LobbyServer.Persistence;

/// <summary>
/// 大厅玩家资料的 Redis 持久化实现；使用注入的 RedisHelper，不拥有连接。
/// </summary>
public sealed class RedisLobbyPlayerStore : ILobbyPlayerStore
{
    private readonly RedisHelper _redis;
    private readonly string _key;

    /// <summary>
    /// 使用外部注入的 Redis 连接创建玩家资料存储。
    /// </summary>
    /// <param name="redis">进程级共享的 RedisHelper。</param>
    /// <param name="keyPrefix">Redis key 前缀。</param>
    public RedisLobbyPlayerStore(RedisHelper redis, string keyPrefix)
    {
        _redis = redis;
        _key = $"{keyPrefix}:lobby:players";
    }

    /// <summary>
    /// 读取并校验全部玩家资料；无效条目会从 Redis 删除。
    /// </summary>
    /// <returns>有效的玩家资料。</returns>
    public IReadOnlyList<PlayerInfo> LoadAll()
    {
        var entries = _redis.HashGetAll<PlayerInfo>(_key);
        var result = new List<PlayerInfo>();

        foreach (var (field, player) in entries)
        {
            if (!long.TryParse(field, out var userId) || userId <= 0)
            {
                Log.Warning("[Redis] 忽略无效玩家资料 field={Field}", field);
                _redis.HashDelete(_key, field);
                continue;
            }

            if (player.UserId != userId)
            {
                Log.Warning("[Redis] 玩家字段与 UserId 不一致，使用字段名 field={Field} userId={UserId}",
                    field, player.UserId);
                player.UserId = userId;
            }

            result.Add(player);
        }

        return result;
    }

    /// <summary>
    /// 保存玩家资料；Redis 异常记录日志，不抛出。
    /// </summary>
    /// <param name="player">玩家资料。</param>
    public void Save(PlayerInfo player)
    {
        if (player.UserId <= 0)
        {
            Log.Warning("[Redis] 跳过无效玩家资料 userId={UserId}", player.UserId);
            return;
        }

        try
        {
            _redis.HashSet(_key, player.UserId.ToString(), player);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Redis] 保存玩家资料失败 userId={UserId}", player.UserId);
        }
    }

    /// <summary>
    /// 删除玩家资料；Redis 异常记录日志，不抛出。
    /// </summary>
    /// <param name="userId">玩家 ID。</param>
    public void Delete(long userId)
    {
        if (userId <= 0)
            return;

        try
        {
            _redis.HashDelete(_key, userId.ToString());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Redis] 删除玩家资料失败 userId={UserId}", userId);
        }
    }
}
