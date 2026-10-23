using SharedLib.Redis;

namespace GameServer.Tests;

public class RedisHelperTests : IDisposable
{
    private readonly string _prefix = $"test:redis:{Guid.NewGuid():N}";
    private readonly RedisHelper _redis = new("localhost:6379");

    [Fact]
    public void HashSetGetGetAllDelete_RoundTrip()
    {
        var key = $"{_prefix}:hash";

        Assert.True(_redis.HashSet(key, "a", new TestValue { Value = 1 }));
        Assert.True(_redis.HashSet(key, "b", new TestValue { Value = 2 }));

        var a = _redis.HashGet<TestValue>(key, "a");
        Assert.NotNull(a);
        Assert.Equal(1, a!.Value);

        var all = _redis.HashGetAll<TestValue>(key);
        Assert.Equal(2, all.Count);
        Assert.Equal(2, all["b"].Value);

        Assert.True(_redis.HashDelete(key, "a"));
        Assert.Null(_redis.HashGet<TestValue>(key, "a"));
        Assert.True(_redis.Delete(key));
        Assert.Empty(_redis.HashGetAll<TestValue>(key));
    }

    [Fact]
    public void ScanKeys_FindsKeysByPattern()
    {
        var key = $"{_prefix}:scan:1";
        _redis.HashSet(key, "f", new TestValue { Value = 1 });

        var keys = _redis.ScanKeys($"{_prefix}:scan:*").ToList();

        Assert.Contains(key, keys);
    }

    [Fact]
    public void Ping_ReturnsTrue()
    {
        Assert.True(_redis.Ping());
    }

    public void Dispose()
    {
        foreach (var key in _redis.ScanKeys($"{_prefix}:*"))
            _redis.Delete(key);
        _redis.Dispose();
    }

    public sealed class TestValue
    {
        public int Value { get; set; }
    }
}
