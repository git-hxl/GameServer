using Serilog;
using SharedLib.Config;

Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("lobby_server_log.txt",
    rollingInterval: RollingInterval.Day,
    rollOnFileSizeLimit: true).CreateLogger();

Log.Information("正在启动 LobbyServer");

var config = ConfigLoader.Load<LobbyServerConfig>("LobbyServerConfig.json");
var server = new LobbyServer.LobbyServer(config);
server.Start(config.Port);

using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(config.UpdateTime));

while (await timer.WaitForNextTickAsync())
{
    server.PollEvents();
}
