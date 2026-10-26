using Serilog;
using SharedLib.Config;
using SharedLib.Redis;

Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("lobby_server_log.txt",
    rollingInterval: RollingInterval.Day,
    rollOnFileSizeLimit: true).CreateLogger();

Log.Information("正在启动 LobbyServer");

var config = ConfigLoader.Load<LobbyServerConfig>("LobbyServerConfig.json");

// 组合根只负责创建 RedisHelper；玩家和房间存储由 LobbyServer.Start 创建。
RedisHelper redis;

try
{
    redis = new RedisHelper(config.RedisConnection, config.RedisDatabase);
}
catch (Exception ex)
{
    Log.Fatal(ex, "[LobbyServer] Redis 初始化失败，请检查 Redis 连接");
    Environment.ExitCode = 1;
    return;
}

LobbyServer.LobbyServer? server = null;

try
{
    server = new LobbyServer.LobbyServer(config, redis);
    server.Start(config);
}
catch (Exception ex)
{
    Log.Fatal(ex, "[LobbyServer] 大厅服务器初始化失败");
    server?.Stop();
    redis.Dispose();
    Environment.ExitCode = 1;
    return;
}

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
finally
{
    server.Stop();
    redis.Dispose();
}

Log.Information("[LobbyServer] 已退出");
