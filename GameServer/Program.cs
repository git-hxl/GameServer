using Newtonsoft.Json;
using Serilog;

namespace GameServer
{
    internal class Program
    {
        static void Main(string[] args)
        {

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration.WriteTo.File("./log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).WriteTo.Console();
            loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();

            GameServerConfig? config = JsonConvert.DeserializeObject<GameServerConfig>(File.ReadAllText("./ServerConfig.json"));
            if (config == null)
                return;

            GameApplication.Instance.Init(config);

            while (true)
            {
                GameApplication.Instance.Update();
                Thread.Sleep(15);
            }
        }
    }
}