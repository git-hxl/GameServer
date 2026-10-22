# AGENTS.md

.NET 10（net10.0）游戏网络后端。三个可执行程序 + 一个共享库 + 一个集成测试项目。无 CI。

## 构建与运行

```sh
dotnet build GameServer.sln            # 验证构建；预期 0 警告
dotnet test GameServer.Tests           # 运行集成测试（真实起服务，走协议流程）
dotnet run --project LobbyServer       # 必须先启动（clientPort=6001，serverPort=6002）
dotnet run --project GameServer        # 启动时连接 LobbyServer 并注册自身（port=7001）
dotnet run --project TestClient        # 交互式命令行客户端（连接 :6001）
```

启动顺序有依赖：LobbyServer → GameServer → TestClient。GameServer 断线后每 5 秒自动重连 LobbyServer；在至少一个 GameServer 注册之前无法创建房间。

连接 key 分角色：客户端用 `ClientConnectionKey`（客户端口/游戏端口），GameServer 用 `ServerConnectionKey`（serverPort），互不通用。

两个服务端 Program.cs 都注册了 `Console.CancelKeyPress` 优雅停机。

## 项目结构

- `SharedLib/` — 共享模型、`MessageIds`、`ReturnCode`、`MessageHelper`、`SharedLib/Handlers`（IHandler/HandlerRegistry/MessageHandler 抽象）、配置加载。网络相关依赖包都在这里（LiteNetLib、MessagePack、Serilog、Newtonsoft.Json）。
- `LobbyServer/` — 大厅与房间管理（入口：`LobbyServer/Program.cs` → `LobbyServer.cs`）。
- `GameServer/` — 游戏内房间处理（入口：`GameServer/Program.cs` → `GameServer.cs`）。
- `TestClient/` — CLI 手动测试客户端，可走完整大厅→房间→游戏流程。
- `GameServer.Tests/` — xunit 集成测试，真实启动 LobbyServer+GameServer，用协议客户端走完整流程。

配置文件 `LobbyServerConfig.json` / `GameServerConfig.json` 会被复制到输出目录（`CopyToOutputDirectory`）。LobbyServer 按 `ClientConnectionKey`/`ServerConnectionKey` 双端口双 key 校验，GameServer 按 `ClientConnectionKey` 接收客户端、`LobbyConnectionKey` 连大厅。

## 网络协议（新增消息时）

消息是 LiteNetLib UDP 二进制格式：`ushort messageId` + `byte returnCode` + MessagePack 序列化的 payload（LZ4 压缩）。发送统一走 `MessageHelper`：`CreateFrame(messageId, code, data)` 构建一次序列化好的 `MessageFrame`，`Send(peer, frame, method)` 单发、`SendToAll(peers, frame, method)` 广播复用；便捷重载 `Send(peer, messageId, code, data?, method)` 自动组装。高频同步消息用 `DeliveryMethod.Sequenced`，控制消息默认 `ReliableOrdered`。接收统一走 `MessageHelper.ReadFrame(reader)` → `MessageFrame`（MessageId/Code/Payload），反序列化用 `MessageHelper.Deserialize<T>(frame)`（全局 options + LZ4）。

新增一条消息需要改四处：
1. 在 `SharedLib/Protocol/MessageIds.cs` 添加常量（客户端消息 <100，服务端内部消息 ≥100）。
2. 在 `SharedLib/Models/` 添加请求/响应模型。
3. 添加实现 `SharedLib.Handlers.IHandler` 的 handler，继承 `MessageHandler<TRequest>` 抽象基类（自动反序列化 + `TryAuthorize`/`OnUnauthorized` 守卫 + 失败回调），暴露 `MessageId` 和 `HandleMessage(NetPeer, TRequest)`。LobbyServer 侧若需「已登录」前置校验，继承 `LobbyStateHandler<TRequest>`。
4. 在 `LobbyServer.cs` 的 `RegisterHandlers()` 或 `GameServer.cs` 的 `RegisterHandlers()` 中注册到对应的 `HandlerRegistry`。

LobbyServer 维护两个 registry：`_clientRegistry`（客户端口消息）和 `_serverRegistry`（GameServer 消息）。GameServer 维护 `_gameRegistry`（游戏客户端）和 `_lobbyRegistry`（LobbyServer，例如 `CreateGameRoom`、`CloseGameRoom`）。

房间相关内部消息：`CreateGameRoom = 102`、`CloseGameRoom = 103`；客户端通知：`RoomOwnerChangedNotify = 16`、`RoomClosedNotify = 17`。

大厅房间是组队房，没有准备/开始/授权流程：
- 玩家 `CreateRoom` 时自动入房并成为房主；外部/系统通过 `LobbyServer.CreateSystemRoom` 创建空房，第一个 `JoinRoom` 的玩家成为房主。
- 建房时 LobbyServer 立即向选中的 GameServer 发 `CreateGameRoom`（幂等）；房间空了或被 `LobbyServer.CloseRoom` 关闭时发 `CloseGameRoom`，GameServer 通知并移出仍在游戏内的玩家。
- 玩家拿到 `JoinRoomResponse` 里的 GameServer 地址后直接连游戏服发 `JoinGame`，GameServer 不校验授权。
- 房主离开时按 `PlayerIds` 加入顺序转移给下一个成员，并广播 `RoomOwnerChangedNotify`。
- `MaxPlayers` 只在 Lobby 层校验；GameServer 不限制人数。
- 玩家离开/切换大厅房间时由客户端先发 `LeaveGame`；GameServer 不会因为游戏房间暂时为空而删除房间，删除只由 Lobby 的 `CloseGameRoom` 触发。

## 约定

- handler 很薄：继承 `MessageHandler<TRequest>`，委托给 `LobbyManager` / `RoomManager`（LobbyServer）或 `GameRoomManager`（GameServer）。房间/玩家状态放在这些 manager 里，而不是 handler 里。
- 服务端注释和日志是中文，修改相邻代码时保持该风格。
- 玩家标识是 `long UserId`；`PlayerManager` 维护 `NetPeer → userId` 映射，并跟踪 `PlayerState`（InLobby/InRoom）。
- 房间流程：`RoomManager.PickGameServer` 根据每个 GameServer 周期性 `GameServerUpdate` 上报的 `GameServerInfo.PlayerCount` 做负载均衡。`GameServerRegistry` 以 `Address:Port`（GameServer 上报的广告地址）为唯一 key 定位 GameServer。
- GameServer 广告地址：`GameServerInfo.Address` 由 GameServer 在 Register/Update 时自行上报——取 `GameServerConfig.PublicAddress`（可配置覆盖），为空时 `AddressResolver` 自动探测本机首个非回环 IPv4，兜底 127.0.0.1。
