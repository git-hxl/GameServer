using LobbyServer.Persistence;
using SharedLib.Models;
using SharedLib.Redis;

namespace GameServer.Tests;

public class LobbyRoomStoreTests : IDisposable
{
    private readonly string _prefix = $"test:roomstore:{Guid.NewGuid():N}";
    private readonly RedisHelper _redis = new("localhost:6379");
    private readonly RedisLobbyRoomStore _store;

    public LobbyRoomStoreTests()
    {
        _store = new RedisLobbyRoomStore("localhost:6379", _prefix);
    }

    [Fact]
    public void SaveLoadDelete_RoundTrip()
    {
        var snapshot = BuildSnapshot("room-1");

        _store.Save(snapshot);

        var loaded = _store.LoadAll().Single();
        Assert.Equal("room-1", loaded.RoomId);
        Assert.Equal(4, loaded.MaxPlayers);
        Assert.Equal(42L, loaded.OwnerUserId);
        Assert.Equal("127.0.0.1", loaded.GameServerAddress);
        Assert.Equal(2, loaded.Players.Count);
        Assert.Equal(42L, loaded.Players[0].UserId);
        Assert.Equal(43L, loaded.Players[1].UserId);

        _store.Delete("room-1");
        Assert.Empty(_store.LoadAll());
    }

    [Fact]
    public void LoadAll_SkipsCorruptJson()
    {
        var key = $"{_prefix}:lobby:rooms";
        _redis.Database.HashSet(key, "bad", "{not-json");

        Assert.Empty(_store.LoadAll());
    }

    [Fact]
    public void Save_WhenStoreDisposed_DoesNotThrow()
    {
        _store.Dispose();

        var exception = Record.Exception(() => _store.Save(BuildSnapshot("room-after-dispose")));

        Assert.Null(exception);
    }

    private static LobbyRoomSnapshot BuildSnapshot(string roomId)
    {
        return new LobbyRoomSnapshot
        {
            RoomId = roomId,
            RoomType = RoomType.Default,
            MaxPlayers = 4,
            OwnerUserId = 42,
            CreatorUserId = 42,
            GameServerAddress = "127.0.0.1",
            GameServerPort = 7001,
            Players =
            [
                new PlayerInfo { UserId = 42, Nickname = "owner" },
                new PlayerInfo { UserId = 43, Nickname = "member" }
            ]
        };
    }

    public void Dispose()
    {
        foreach (var key in _redis.ScanKeys($"{_prefix}:*"))
            _redis.Delete(key);
        _store.Dispose();
        _redis.Dispose();
    }
}
