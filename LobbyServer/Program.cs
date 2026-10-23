using LobbyServer.Hosting;
using Serilog;
using SharedLib.Config;

Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("lobby_server_log.txt",
    rollingInterval: RollingInterval.Day,
    rollOnFileSizeLimit: true).CreateLogger();

Log.Information("正在启动 LobbyServer");

var config = ConfigLoader.Load<LobbyServerConfig>("LobbyServerConfig.json");

// 进程入口只负责日志、配置和调用大厅 Host；Redis/房间逻辑都在 LobbyServer 项目内。
await LobbyServerHost.RunAsync(config);

Log.Information("[LobbyServer] 已退出");
