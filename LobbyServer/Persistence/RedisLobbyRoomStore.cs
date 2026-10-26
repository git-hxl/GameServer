using LobbyServer.Room;
using Serilog;
using SharedLib.Redis;

namespace LobbyServer.Persistence;

/// <summary>
/// 大厅房间的 Redis 持久化实现；直接序列化 LobbyRoom，使用注入的 RedisHelper。
/// </summary>
public sealed class RedisLobbyRoomStore : ILobbyRoomStore
{
    private readonly RedisHelper _redis;
    private readonly string _key;

    /// <summary>
    /// 使用外部注入的 Redis 连接创建大厅房间存储；不拥有该连接。
    /// </summary>
    /// <param name="redis">进程级共享的 RedisHelper。</param>
    /// <param name="keyPrefix">Redis key 前缀。</param>
    public RedisLobbyRoomStore(RedisHelper redis, string keyPrefix)
    {
        _redis = redis;
        _key = $"{keyPrefix}:lobby:rooms";
    }

    /// <summary>
    /// 读取并校验全部大厅房间；无效条目会从 Redis 删除。
    /// </summary>
    /// <returns>有效的大厅房间。</returns>
    public IReadOnlyList<LobbyRoom> LoadAll()
    {
        var entries = _redis.HashGetAll<LobbyRoom>(_key);
        var result = new List<LobbyRoom>();

        foreach (var (field, room) in entries)
        {
            if (string.IsNullOrWhiteSpace(field) ||
                string.IsNullOrWhiteSpace(room.GameServerAddress) ||
                room.GameServerPort <= 0)
            {
                Log.Warning("[Redis] 忽略无效房间数据 field={Field}", field);
                if (!string.IsNullOrWhiteSpace(field))
                    _redis.HashDelete(_key, field);
                continue;
            }

            if (!string.Equals(room.RoomId, field, StringComparison.Ordinal))
            {
                Log.Warning("[Redis] 房间字段与 RoomId 不一致，使用字段名 field={Field} roomId={RoomId}",
                    field, room.RoomId);
                room.RoomId = field;
            }

            if (room.PlayerIds == null)
                room.PlayerIds = [];

            result.Add(room);
        }

        return result;
    }

    /// <summary>
    /// 保存单个大厅房间；Redis 异常记录日志，不抛出。
    /// </summary>
    /// <param name="room">大厅房间。</param>
    public void Save(LobbyRoom room)
    {
        try
        {
            _redis.HashSet(_key, room.RoomId, room);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Redis] 保存房间失败 roomId={RoomId}", room.RoomId);
        }
    }

    /// <summary>
    /// 删除大厅房间；Redis 异常记录日志，不抛出。
    /// </summary>
    /// <param name="roomId">房间 ID。</param>
    public void Delete(string roomId)
    {
        try
        {
            _redis.HashDelete(_key, roomId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Redis] 删除房间失败 roomId={RoomId}", roomId);
        }
    }
}
