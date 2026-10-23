using Serilog;
using SharedLib.Redis;

namespace LobbyServer.Persistence;

public sealed class RedisLobbyRoomStore : ILobbyRoomStore
{
    private readonly RedisHelper _redis;
    private readonly string _key;
    private bool _disposed;

    public RedisLobbyRoomStore(string connectionString, string keyPrefix, int database = 0)
    {
        _redis = new RedisHelper(connectionString, database);
        _key = $"{keyPrefix}:lobby:rooms";
    }

    public IReadOnlyList<LobbyRoomSnapshot> LoadAll()
    {
        var entries = _redis.HashGetAll<LobbyRoomSnapshot>(_key);
        var result = new List<LobbyRoomSnapshot>();

        foreach (var (field, snapshot) in entries)
        {
            if (string.IsNullOrWhiteSpace(field) ||
                snapshot.Version != 1 ||
                string.IsNullOrWhiteSpace(snapshot.GameServerAddress) ||
                snapshot.GameServerPort <= 0)
            {
                Log.Warning("[Redis] 忽略无效房间快照 field={Field}", field);
                if (!string.IsNullOrWhiteSpace(field))
                    _redis.HashDelete(_key, field);
                continue;
            }

            if (!string.Equals(snapshot.RoomId, field, StringComparison.Ordinal))
            {
                Log.Warning("[Redis] 房间字段与 RoomId 不一致，使用字段名 field={Field} roomId={RoomId}",
                    field, snapshot.RoomId);
                snapshot.RoomId = field;
            }

            if (snapshot.Players == null)
                snapshot.Players = [];

            result.Add(snapshot);
        }

        return result;
    }

    public void Save(LobbyRoomSnapshot room)
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

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _redis.Dispose();
    }
}
