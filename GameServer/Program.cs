using Serilog;
using SharedLib.Config;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("game_server_log.txt",
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true)
    .CreateLogger();

Log.Information("正在启动 GameServer");

var config = ConfigLoader.Load<GameServerConfig>("GameServerConfig.json");
var server = new GameServer.GameServer(config);
server.Start();

using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(config.UpdateTime));

while (await timer.WaitForNextTickAsync())
{
    server.PollEvents();
}
