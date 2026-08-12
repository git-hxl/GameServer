using Newtonsoft.Json;
using Serilog;

namespace SharedLib.Config
{

    /// <summary>
    /// JSON 配置文件的加载与保存工具类
    /// </summary>
    public static class ConfigLoader
    {
        private const string DefaultFileName = "server_config.json";

        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            Formatting = Formatting.Indented
        };

        /// <summary>
        /// 从文件加载配置，不存在时自动生成默认配置并保存
        /// </summary>
        public static T Load<T>(string? path = null) where T : new()
        {
            var filePath = path ?? Path.Combine(AppContext.BaseDirectory, DefaultFileName);

            if (!File.Exists(filePath))
            {
                var defaults = new T();
                Save(filePath, defaults);
                Log.Information("配置文件不存在，已生成默认配置: {Path}", filePath);
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var config = JsonConvert.DeserializeObject<T>(json);
                if (config != null)
                {
                    Log.Information("配置文件加载成功: {Path}", filePath);
                    return config;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "配置文件解析失败，使用默认值: {Path}", filePath);
            }

            return new T();
        }

        /// <summary>
        /// 将配置对象序列化为 JSON 并写入文件
        /// </summary>
        private static void Save<T>(string path, T config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config, SerializerSettings);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "配置保存失败: {Path}", path);
            }
        }
    }
}
