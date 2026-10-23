using LobbyServer.Persistence;
using Serilog;
using SharedLib.Config;

Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("lobby_server_log.txt",
    rollingInterval: RollingInterval.Day,
    rollOnFileSizeLimit: true).CreateLogger();

Log.Information("正在启动 LobbyServer");

var config = ConfigLoader.Load<LobbyServerConfig>("LobbyServerConfig.json");

// 组合根负责 Redis 连接与存储的创建/释放；LobbyServer 只使用注入的 store。
ILobbyRoomStore? roomStore = null;
LobbyServer.LobbyServer? server = null;

try
{
    roomStore = new RedisLobbyRoomStore(
        config.RedisConnection, config.RedisKeyPrefix, config.RedisDatabase);

    server = new LobbyServer.LobbyServer(config, roomStore);
    server.Start(config);
}
catch (Exception ex)
{
    Log.Fatal(ex, "[LobbyServer] 初始化失败，请检查 Redis 连接");
    roomStore?.Dispose();
    Environment.ExitCode = 1;
    return;
}

var lobbyServer = server!;
var store = roomStore!;

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
        lobbyServer.PollEvents();
    }
}
catch (OperationCanceledException)
{
}
finally
{
    lobbyServer.Stop();
    store.Dispose();
}

Log.Information("[LobbyServer] 已退出");
