using LobbyServer.Persistence;
using Serilog;
using SharedLib.Config;
using LobbyServerCore = LobbyServer.LobbyServer;

namespace LobbyServer.Hosting;

/// <summary>
/// 大厅进程的组合根：负责 Redis 存储、LobbyServer、轮询循环和停机。
/// </summary>
internal static class LobbyServerHost
{
    /// <summary>
    /// 创建 Redis 房间存储并启动大厅服务器，运行到收到停机信号。
    /// </summary>
    /// <param name="config">大厅服务器配置。</param>
    public static async Task RunAsync(LobbyServerConfig config)
    {
        ILobbyRoomStore? roomStore = null;
        LobbyServerCore? server = null;

        try
        {
            // 大厅房间持久化属于大厅层：在这里创建 store，Redis 连接由 store 内部完成。
            roomStore = new RedisLobbyRoomStore(
                config.RedisConnection, config.RedisKeyPrefix, config.RedisDatabase);

            server = new LobbyServerCore(config, roomStore);
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
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[LobbyServer] 初始化失败，请检查 Redis 连接");
            Environment.ExitCode = 1;
        }
        finally
        {
            server?.Stop();
            roomStore?.Dispose();
        }
    }
}
