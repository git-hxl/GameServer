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
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Log.Information("[GameServer] 收到停机信号，准备退出");
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

Log.Information("[GameServer] 已退出");
