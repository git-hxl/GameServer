using Serilog;
using StackExchange.Redis;

namespace SharedLibrary
{
    public class RedisManager
    {
        public static RedisManager? Instance { get; set; }

        private ConnectionMultiplexer redis;

        public RedisManager(string connectStr)
        {
            redis = ConnectionMultiplexer.Connect(connectStr);
        }

        public bool IsConnected { get { return redis.IsConnected; } }

        public string? StringGet(string key)
        {
            if (redis != null && redis.IsConnected)
            {
                return redis.GetDatabase().StringGet(key);
            }
            Log.Error("Redis is Null or is not Connected!!!");
            return null;
        }

        public bool StringSet(string key, string value)
        {
            if (redis != null && redis.IsConnected)
            {
                return redis.GetDatabase().StringSet(key, value);
            }
            Log.Error("Redis is Null or is not Connected!!!");
            return false;
        }

        public string? StringGet(int db,string key)
        {
            if (redis != null && redis.IsConnected)
            {
                return redis.GetDatabase(db).StringGet(key);
            }
            Log.Error("Redis is Null or is not Connected!!!");
            return null;
        }

        public bool StringSet(int db,string key, string value)
        {
            if (redis != null && redis.IsConnected)
            {
                return redis.GetDatabase(db).StringSet(key, value);
            }
            Log.Error("Redis is Null or is not Connected!!!");
            return false;
        }
    }
}