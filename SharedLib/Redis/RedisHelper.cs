using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;

namespace SharedLib.Redis;

/// <summary>
/// Redis 连接与通用读写封装
/// </summary>
public sealed class RedisHelper : IDisposable
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly ConnectionMultiplexer _connection;
    private readonly IDatabase _database;

    public RedisHelper(string connectionString, int database = 0)
    {
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = true;
        options.ConnectTimeout = 5000;

        _connection = ConnectionMultiplexer.Connect(options);
        _database = _connection.GetDatabase(database);
    }

    public IDatabase Database => _database;

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

    public T? HashGet<T>(string key, string field)
    {
        try
        {
            var value = _database.HashGet(key, field);
            if (value.IsNullOrEmpty)
                return default;

            return JsonConvert.DeserializeObject<T>(value.ToString(), JsonSettings);
        }
        catch (JsonException ex)
        {
            Log.Warning(ex, "[Redis] 反序列化失败 key={Key} field={Field}", key, field);
            return default;
        }
    }

    public IReadOnlyDictionary<string, T> HashGetAll<T>(string key)
    {
        var result = new Dictionary<string, T>();

        foreach (var entry in _database.HashGetAll(key))
        {
            if (entry.Value.IsNullOrEmpty)
                continue;

            try
            {
                var value = JsonConvert.DeserializeObject<T>(entry.Value.ToString(), JsonSettings);
                if (value != null)
                    result[entry.Name.ToString()] = value;
            }
            catch (JsonException ex)
            {
                Log.Warning(ex, "[Redis] 反序列化失败 key={Key} field={Field}", key, entry.Name.ToString());
            }
        }

        return result;
    }

    public bool HashSet<T>(string key, string field, T value)
    {
        var json = JsonConvert.SerializeObject(value, JsonSettings);
        return _database.HashSet(key, field, json);
    }

    public bool HashDelete(string key, string field)
    {
        return _database.HashDelete(key, field);
    }

    public bool Delete(string key)
    {
        return _database.KeyDelete(key);
    }

    public IEnumerable<string> ScanKeys(string pattern)
    {
        var endpoint = _connection.GetEndPoints().FirstOrDefault();
        if (endpoint == null)
            yield break;

        foreach (var key in _connection.GetServer(endpoint).Keys(pattern: pattern, pageSize: 250))
            yield return key.ToString();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
