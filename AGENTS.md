# AGENTS.md

.NET 10（net10.0）游戏网络后端。三个可执行程序 + 一个共享库。无 CI。

## 构建与运行

```sh
dotnet build GameServer.sln            # 验证构建；预期 0 警告 0 错误
dotnet run --project LobbyServer       # 必须先启动（clientPort=6001，serverPort=6002）
dotnet run --project GameServer        # 启动时连接 LobbyServer 并注册自身（port=7001）
dotnet run --project TestClient        # 交互式命令行客户端（连接 :6001）
```

当前快速开发阶段不维护自动化测试项目；用 `dotnet build` + 手动启动 LobbyServer/GameServer/TestClient 验证。

启动顺序有依赖：Redis → LobbyServer → GameServer → TestClient。LobbyServer 启动时必须能连接 Redis（默认 `localhost:6379`），否则直接退出。

连接 key 分角色：客户端用 `ClientConnectionKey`（客户端口/游戏端口），GameServer 用 `ServerConnectionKey`（serverPort），互不通用。

两个服务端 Program.cs 都注册了 `Console.CancelKeyPress` 优雅停机。

## 项目结构

- `DESIGN.md` — 当前架构、协议、玩家/房间模型、Redis、配置与限制的设计文档；**功能改动/新增必须同步更新**。
- `SharedLib/` — 共享模型、`MessageIds`、`ReturnCode`、`MessageHelper`、`SharedLib/Handlers`（IHandler/HandlerRegistry/MessageHandler 抽象）、配置加载。网络相关依赖包都在这里（LiteNetLib、MessagePack、Serilog、Newtonsoft.Json、StackExchange.Redis）。
- `LobbyServer/` — 大厅与房间管理（入口：`LobbyServer/Program.cs` → `LobbyServer.cs`）。
- `LobbyServer/Handlers/Client/` — 客户端消息 handler（含 `LobbyStateHandler`）。
- `LobbyServer/Handlers/Server/` — GameServer 内部消息 handler（注册、更新）。
- `GameServer/` — 游戏内房间处理（入口：`GameServer/Program.cs` → `GameServer.cs`）。
- `TestClient/` — CLI 手动测试客户端，可走完整大厅→房间→游戏流程。

配置文件 `LobbyServerConfig.json` / `GameServerConfig.json` 会被复制到输出目录（`CopyToOutputDirectory`）。LobbyServer 按 `ClientConnectionKey`/`ServerConnectionKey` 双端口双 key 校验，GameServer 按 `ClientConnectionKey` 接收客户端、`LobbyConnectionKey` 连大厅。

## 开发约定

- 新增一条消息的流程：在 `MessageIds` 添加 ID → 在 `Models` 添加请求/响应模型 → 添加 handler（继承 `MessageHandler<TRequest>`，Lobby 侧需要登录态时继承 `LobbyStateHandler<TRequest>`）→ 在对应的 `RegisterHandlers()` 中注册。
- handler 保持很薄：只做解析/转发，房间和玩家状态放在 `PlayerManager` / `RoomManager` / `GameRoomManager`。
- 服务端注释和日志使用中文，修改相邻代码时保持该风格。
- 所有方法必须写注释：方法（含 `private`/`static`/局部函数、构造函数）使用中文 XML 文档注释；接口实现/重写可用 `<inheritdoc/>`。新增/修改方法时注释必须与代码同一提交完成。
- 根目录 `DESIGN.md` 是当前实现的设计文档；任何协议、状态模型、配置、Redis、重连/停机语义的功能改动或新增，必须在同一提交中更新对应章节，并按第 19 节检查清单自检。
