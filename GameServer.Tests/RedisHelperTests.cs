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
        Assert.True(_redis.IsConnected);
    }

    [Fact]
    public void StringAndKeyOperations_RoundTrip()
    {
        var key = $"{_prefix}:string";
        var counter = $"{_prefix}:counter";

        Assert.True(_redis.StringSet(key, new TestValue { Value = 7 }, TimeSpan.FromSeconds(30)));
        Assert.Equal(7, _redis.StringGet<TestValue>(key)!.Value);
        Assert.True(_redis.KeyExists(key));
        Assert.NotNull(_redis.KeyTimeToLive(key));
        Assert.True(_redis.KeyExpire(key, TimeSpan.FromSeconds(60)));
        Assert.Equal(3, _redis.StringIncrement(counter, 3));
        Assert.Equal(3, _redis.StringIncrement(counter, 0));
        Assert.True(_redis.Delete(key));
        Assert.False(_redis.KeyExists(key));
    }

    [Fact]
    public void HashOperations_Work()
    {
        var key = $"{_prefix}:hash2";

        Assert.True(_redis.HashSet(key, "a", new TestValue { Value = 1 }));
        Assert.True(_redis.HashExists(key, "a"));
        Assert.Equal(1, _redis.HashLength(key));
        Assert.Contains("a", _redis.HashKeys(key));
        Assert.Equal(5, _redis.HashIncrement(key, "counter", 5));
        Assert.True(_redis.HashDelete(key, "a"));
        Assert.False(_redis.HashExists(key, "a"));
    }

    [Fact]
    public void HashSetAll_Works()
    {
        var key = $"{_prefix}:hashall";

        _redis.HashSetAll(key, new Dictionary<string, TestValue>
        {
            ["a"] = new() { Value = 1 },
            ["b"] = new() { Value = 2 }
        });

        var all = _redis.HashGetAll<TestValue>(key);
        Assert.Equal(2, all.Count);
        Assert.Equal(2, all["b"].Value);
    }

    [Fact]
    public void ListOperations_Work()
    {
        var key = $"{_prefix}:list";

        _redis.ListRightPush(key, new TestValue { Value = 1 });
        _redis.ListRightPush(key, new TestValue { Value = 2 });
        _redis.ListLeftPush(key, new TestValue { Value = 0 });

        Assert.Equal(3, _redis.ListLength(key));
        var values = _redis.ListRange<TestValue>(key);
        Assert.Equal(new[] { 0, 1, 2 }, values.Select(v => v.Value).ToArray());
        Assert.Equal(1, _redis.ListRemove(key, new TestValue { Value = 1 }));
    }

    [Fact]
    public void SetOperations_Work()
    {
        var key = $"{_prefix}:set";

        Assert.True(_redis.SetAdd(key, new TestValue { Value = 1 }));
        Assert.True(_redis.SetAdd(key, new TestValue { Value = 2 }));
        Assert.True(_redis.SetContains(key, new TestValue { Value = 1 }));
        Assert.Equal(2, _redis.SetLength(key));
        Assert.Equal(2, _redis.SetMembers<TestValue>(key).Count);
        Assert.True(_redis.SetRemove(key, new TestValue { Value = 1 }));
        Assert.False(_redis.SetContains(key, new TestValue { Value = 1 }));
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
