using Serilog;
using SharedLib.Config;

Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("lobby_server_log.txt",
    rollingInterval: RollingInterval.Day,
    rollOnFileSizeLimit: true).CreateLogger();

Log.Information("正在启动 LobbyServer");

var config = ConfigLoader.Load<LobbyServerConfig>("LobbyServerConfig.json");
var server = new LobbyServer.LobbyServer(config);
server.Start(config);

using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(config.UpdateTime));
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Log.Information("[LobbyServer] 收到停机信号，准备退出");
    cts.Cancel();
};

try
{
    while (await timer.WaitForNextTickAsync(cts.Token))
    {
        server.PollEvents();
    }
}
catch (OperationCanceledException)
{
}

Log.Information("[LobbyServer] 已退出");
