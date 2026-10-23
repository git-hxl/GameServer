using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;

namespace SharedLib.Redis;

/// <summary>
/// Redis 连接与通用读写封装。实例应作为进程级单例由组合根创建并注入，避免每次请求新建连接。
/// </summary>
public sealed class RedisHelper : IDisposable
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly ConnectionMultiplexer _connection;
    private readonly IDatabase _database;

    /// <summary>
    /// 创建 Redis 连接并选择逻辑数据库。
    /// </summary>
    /// <param name="connectionString">StackExchange.Redis 连接字符串，例如 localhost:6379。</param>
    /// <param name="database">逻辑数据库编号。</param>
    public RedisHelper(string connectionString, int database = 0)
    {
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = true;
        options.ConnectTimeout = 5000;
        options.SyncTimeout = 1000;

        _connection = ConnectionMultiplexer.Connect(options);
        _database = _connection.GetDatabase(database);
    }

    /// <summary>
    /// 当前是否已连接到 Redis。
    /// </summary>
    public bool IsConnected => _connection.IsConnected;

    /// <summary>
    /// 底层 IDatabase，供高级用法或测试直接调用。
    /// </summary>
    public IDatabase Database => _database;

    /// <summary>
    /// 检查 Redis 是否可用。
    /// </summary>
    /// <returns>Ping 成功返回 true。</returns>
    public bool Ping()
    {
        try
        {
            _database.Ping();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Redis] Ping 失败");
            return false;
        }
    }

    // ── String ────────────────────────────────────────────────────────

    /// <summary>
    /// 读取字符串 key 并反序列化为指定类型。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="key">Redis key。</param>
    /// <returns>反序列化结果；key 不存在或解析失败返回 default。</returns>
    public T? StringGet<T>(string key)
    {
        return Deserialize<T>(_database.StringGet(key), key);
    }

    /// <summary>
    /// 序列化并写入字符串 key。
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="key">Redis key。</param>
    /// <param name="value">要写入的值。</param>
    /// <param name="expiry">可选过期时间。</param>
    /// <returns>写入成功返回 true。</returns>
    public bool StringSet<T>(string key, T value, TimeSpan? expiry = null)
    {
        return _database.StringSet(key, Serialize(value), expiry, When.Always);
    }

    /// <summary>
    /// 对字符串 key 做整数自增。
    /// </summary>
    /// <param name="key">Redis key。</param>
    /// <param name="value">自增量，默认 1。</param>
    /// <returns>自增后的值。</returns>
    public long StringIncrement(string key, long value = 1)
    {
        return _database.StringIncrement(key, value);
    }

    // ── Key ───────────────────────────────────────────────────────────

    /// <summary>
    /// 判断 key 是否存在。
    /// </summary>
    /// <param name="key">Redis key。</param>
    /// <returns>存在返回 true。</returns>
    public bool KeyExists(string key)
    {
        return _database.KeyExists(key);
    }

    /// <summary>
    /// 设置 key 的过期时间。
    /// </summary>
    /// <param name="key">Redis key。</param>
    /// <param name="expiry">过期时长。</param>
    /// <returns>设置成功返回 true。</returns>
    public bool KeyExpire(string key, TimeSpan expiry)
    {
        return _database.KeyExpire(key, expiry);
    }

    /// <summary>
    /// 获取 key 的剩余存活时间。
    /// </summary>
    /// <param name="key">Redis key。</param>
    /// <returns>剩余时间；key 不存在或没有过期时间返回 null。</returns>
    public TimeSpan? KeyTimeToLive(string key)
    {
        return _database.KeyTimeToLive(key);
    }

    /// <summary>
    /// 删除 key。
    /// </summary>
    /// <param name="key">Redis key。</param>
    /// <returns>删除成功返回 true。</returns>
    public bool Delete(string key)
    {
        return _database.KeyDelete(key);
    }

    // ── Hash ──────────────────────────────────────────────────────────

    /// <summary>
    /// 读取 Hash 字段并反序列化。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="key">Hash key。</param>
    /// <param name="field">字段名。</param>
    /// <returns>反序列化结果；不存在或解析失败返回 default。</returns>
    public T? HashGet<T>(string key, string field)
    {
        return Deserialize<T>(_database.HashGet(key, field), key, field);
    }

    /// <summary>
    /// 读取 Hash 的全部字段并反序列化。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="key">Hash key。</param>
    /// <returns>字段名到反序列化值的字典。</returns>
    public IReadOnlyDictionary<string, T> HashGetAll<T>(string key)
    {
        var result = new Dictionary<string, T>();

        foreach (var entry in _database.HashGetAll(key))
        {
            var name = entry.Name.ToString();
            var value = Deserialize<T>(entry.Value, key, name);
            if (value != null)
                result[name] = value;
        }

        return result;
    }

    /// <summary>
    /// 批量写入 Hash 字段。
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="key">Hash key。</param>
    /// <param name="values">字段名到值的映射。</param>
    /// <param name="expiry">可选过期时间。</param>
    public void HashSetAll<T>(string key, IReadOnlyDictionary<string, T> values, TimeSpan? expiry = null)
    {
        if (values.Count == 0)
            return;

        var entries = values
            .Select(pair => new HashEntry(pair.Key, Serialize(pair.Value)))
            .ToArray();

        _database.HashSet(key, entries);

        if (expiry.HasValue)
            _database.KeyExpire(key, expiry);
    }

    /// <summary>
    /// 写入单个 Hash 字段。
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="key">Hash key。</param>
    /// <param name="field">字段名。</param>
    /// <param name="value">字段值。</param>
    /// <returns>新增字段返回 true；覆盖已有字段返回 false。</returns>
    public bool HashSet<T>(string key, string field, T value)
    {
        return _database.HashSet(key, field, Serialize(value));
    }

    /// <summary>
    /// 删除 Hash 字段。
    /// </summary>
    /// <param name="key">Hash key。</param>
    /// <param name="field">字段名。</param>
    /// <returns>字段存在并被删除返回 true。</returns>
    public bool HashDelete(string key, string field)
    {
        return _database.HashDelete(key, field);
    }

    /// <summary>
    /// 判断 Hash 字段是否存在。
    /// </summary>
    /// <param name="key">Hash key。</param>
    /// <param name="field">字段名。</param>
    /// <returns>存在返回 true。</returns>
    public bool HashExists(string key, string field)
    {
        return _database.HashExists(key, field);
    }

    /// <summary>
    /// 获取 Hash 字段数量。
    /// </summary>
    /// <param name="key">Hash key。</param>
    /// <returns>字段数量。</returns>
    public long HashLength(string key)
    {
        return _database.HashLength(key);
    }

    /// <summary>
    /// 获取 Hash 的全部字段名。
    /// </summary>
    /// <param name="key">Hash key。</param>
    /// <returns>字段名集合。</returns>
    public IEnumerable<string> HashKeys(string key)
    {
        return _database.HashKeys(key).Select(value => value.ToString());
    }

    /// <summary>
    /// 对 Hash 字段做整数自增。
    /// </summary>
    /// <param name="key">Hash key。</param>
    /// <param name="field">字段名。</param>
    /// <param name="value">自增量，默认 1。</param>
    /// <returns>自增后的值。</returns>
    public long HashIncrement(string key, string field, long value = 1)
    {
        return _database.HashIncrement(key, field, value);
    }

    // ── List ──────────────────────────────────────────────────────────

    /// <summary>
    /// 从列表左侧插入元素。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="key">List key。</param>
    /// <param name="value">元素值。</param>
    /// <returns>插入后的列表长度。</returns>
    public long ListLeftPush<T>(string key, T value)
    {
        return _database.ListLeftPush(key, Serialize(value));
    }

    /// <summary>
    /// 从列表右侧插入元素。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="key">List key。</param>
    /// <param name="value">元素值。</param>
    /// <returns>插入后的列表长度。</returns>
    public long ListRightPush<T>(string key, T value)
    {
        return _database.ListRightPush(key, Serialize(value));
    }

    /// <summary>
    /// 读取列表区间并反序列化。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="key">List key。</param>
    /// <param name="start">起始索引，默认 0。</param>
    /// <param name="stop">结束索引，默认 -1 表示到末尾。</param>
    /// <returns>反序列化后的元素列表。</returns>
    public IReadOnlyList<T> ListRange<T>(string key, long start = 0, long stop = -1)
    {
        var result = new List<T>();

        foreach (var value in _database.ListRange(key, start, stop))
        {
            var item = Deserialize<T>(value, key);
            if (item != null)
                result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// 获取列表长度。
    /// </summary>
    /// <param name="key">List key。</param>
    /// <returns>列表长度。</returns>
    public long ListLength(string key)
    {
        return _database.ListLength(key);
    }

    /// <summary>
    /// 从列表中删除匹配的元素。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="key">List key。</param>
    /// <param name="value">要删除的元素值。</param>
    /// <param name="count">删除数量；0 表示删除全部匹配项。</param>
    /// <returns>实际删除数量。</returns>
    public long ListRemove<T>(string key, T value, long count = 0)
    {
        return _database.ListRemove(key, Serialize(value), count);
    }

    // ── Set ───────────────────────────────────────────────────────────

    /// <summary>
    /// 向集合添加元素。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="key">Set key。</param>
    /// <param name="value">元素值。</param>
    /// <returns>新增元素返回 true；已存在返回 false。</returns>
    public bool SetAdd<T>(string key, T value)
    {
        return _database.SetAdd(key, Serialize(value));
    }

    /// <summary>
    /// 从集合移除元素。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="key">Set key。</param>
    /// <param name="value">元素值。</param>
    /// <returns>元素存在并被移除返回 true。</returns>
    public bool SetRemove<T>(string key, T value)
    {
        return _database.SetRemove(key, Serialize(value));
    }

    /// <summary>
    /// 判断集合是否包含指定元素。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="key">Set key。</param>
    /// <param name="value">元素值。</param>
    /// <returns>包含返回 true。</returns>
    public bool SetContains<T>(string key, T value)
    {
        return _database.SetContains(key, Serialize(value));
    }

    /// <summary>
    /// 读取集合全部元素并反序列化。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="key">Set key。</param>
    /// <returns>反序列化后的元素列表。</returns>
    public IReadOnlyList<T> SetMembers<T>(string key)
    {
        var result = new List<T>();

        foreach (var value in _database.SetMembers(key))
        {
            var item = Deserialize<T>(value, key);
            if (item != null)
                result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// 获取集合元素数量。
    /// </summary>
    /// <param name="key">Set key。</param>
    /// <returns>元素数量。</returns>
    public long SetLength(string key)
    {
        return _database.SetLength(key);
    }

    // ── 其他 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 按 pattern 扫描 key，仅用于启动恢复、测试或运维排查。
    /// </summary>
    /// <param name="pattern">Redis key 匹配模式，例如 prefix:*。</param>
    /// <returns>匹配的 key 集合。</returns>
    public IEnumerable<string> ScanKeys(string pattern)
    {
        var endpoint = _connection.GetEndPoints().FirstOrDefault();
        if (endpoint == null)
            yield break;

        foreach (var key in _connection.GetServer(endpoint).Keys(pattern: pattern, pageSize: 250))
            yield return key.ToString();
    }

    /// <summary>
    /// 释放 Redis 连接。
    /// </summary>
    public void Dispose()
    {
        _connection.Dispose();
    }

    /// <summary>
    /// 将值序列化为 RedisValue。
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="value">要序列化的值。</param>
    /// <returns>JSON 字节值。</returns>
    private static RedisValue Serialize<T>(T value)
    {
        return JsonConvert.SerializeObject(value, JsonSettings);
    }

    /// <summary>
    /// 将 RedisValue 反序列化为指定类型；坏数据记录日志并返回 default。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="value">Redis 原始值。</param>
    /// <param name="key">用于日志的 key。</param>
    /// <param name="field">用于日志的 Hash 字段。</param>
    /// <returns>反序列化结果或 default。</returns>
    private static T? Deserialize<T>(RedisValue value, string key, string? field = null)
    {
        if (value.IsNullOrEmpty)
            return default;

        try
        {
            return JsonConvert.DeserializeObject<T>(value.ToString(), JsonSettings);
        }
        catch (JsonException ex)
        {
            if (field == null)
                Log.Warning(ex, "[Redis] 反序列化失败 key={Key}", key);
            else
                Log.Warning(ex, "[Redis] 反序列化失败 key={Key} field={Field}", key, field);

            return default;
        }
    }
}
