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
        return _redis.HashGetAll<LobbyRoomSnapshot>(_key).Values.ToList();
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
