using Newtonsoft.Json;
using Serilog;
namespace MasterServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration.WriteTo.File("./log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).WriteTo.Console();
            loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();

            MasterServerConfig? config = JsonConvert.DeserializeObject<MasterServerConfig>(File.ReadAllText("./ServerConfig.json"));
            if (config == null)
                return;

            MasterApplication.Instance.Init(config);

            while (true)
            {
                MasterApplication.Instance.Update();
                Thread.Sleep(15);
            }
        }
    }
}