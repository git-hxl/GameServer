using Newtonsoft.Json;
using Serilog;
using SharedLibrary.Server;

namespace GameServer
{
    internal class GameApplication
    {
        public static ServerConfig ServerConfig { get; private set; }

        static void Main(string[] args)
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration.WriteTo.File("./log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).WriteTo.Console();
            loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();

            ServerConfig = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText("./ServerConfig.json"));

            GameServer gameServer = new GameServer(ServerConfig);

            gameServer.Start(ServerConfig.port);

            while (true)
            {
                gameServer.Update();
                Thread.Sleep(15);
            }
        }
    }
}