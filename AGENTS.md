# AGENTS.md

.NET 10（net10.0）游戏网络后端。三个可执行程序 + 一个共享库。无测试、无 CI、无 README。

## 构建与运行

```sh
dotnet build GameServer.sln        # 验证构建；预期 0 警告
dotnet run --project LobbyServer   # 必须先启动（clientPort=6001，serverPort=6002）
dotnet run --project GameServer    # 启动时连接 LobbyServer 并注册自身（port=7001）
dotnet run --project TestClient    # 交互式命令行客户端（连接 :6001）
```

启动顺序有依赖：LobbyServer → GameServer → TestClient。GameServer 断线后每 5 秒自动重连 LobbyServer；在至少一个 GameServer 注册之前无法创建房间。

连接 key 分角色：客户端用 `ClientConnectionKey`（客户端口/游戏端口），GameServer 用 `ServerConnectionKey`（serverPort），互不通用。

## 项目结构

- `SharedLib/` — 共享模型、`MessageIds`、`ReturnCode`、`MessageHelper`、配置加载。两个服务端和 TestClient 都引用它。网络相关依赖包都在这里（LiteNetLib、MessagePack、Serilog、Newtonsoft.Json）。
- `LobbyServer/` — 大厅与房间管理（入口：`LobbyServer/Program.cs` → `LobbyServer.cs`）。
- `GameServer/` — 游戏内房间处理（入口：`GameServer/Program.cs` → `GameServer.cs`）。
- `TestClient/` — CLI 手动测试客户端，可走完整大厅→房间→游戏流程。

配置文件 `LobbyServerConfig.json` / `GameServerConfig.json` 会被复制到输出目录（`CopyToOutputDirectory`）。双方 `ConnectionKey` 必须一致（`Game@wasd9527`）；客户端通过 `request.AcceptIfKey` 校验连接。

## 网络协议（新增消息时）

消息是 LiteNetLib UDP 二进制格式：`ushort messageId` + `byte returnCode` + MessagePack 序列化的 payload。用 `MessageHelper.Send(peer, messageId, code, data)` 组帧；高频同步消息用 `DeliveryMethod.Sequenced`，控制消息默认 `ReliableOrdered`。帧解析统一走 `MessageHelper.ReadFrame(reader)`。

新增一条消息需要改四处：
1. 在 `SharedLib/Protocol/MessageIds.cs` 添加常量（客户端消息 <100，服务端内部消息 ≥100）。
2. 在 `SharedLib/Models/` 添加请求/响应模型。
3. 添加实现 `SharedLib.Handlers.IHandler` 的 handler，继承 `MessageHandler<TRequest>` 抽象基类（自动反序列化 + 失败回调），暴露 `MessageId` 和 `HandleMessage(NetPeer, TRequest)`。
4. 在 `LobbyServer.cs` 的 `RegisterHandlers()` 或 `GameServer.cs` 的 `RegisterHandlers()` 中注册到对应的 `HandlerRegistry`。

LobbyServer 维护两个 registry：`_clientRegistry`（客户端口消息）和 `_serverRegistry`（GameServer 消息）。GameServer 维护 `_gameRegistry`（游戏客户端）和 `_lobbyRegistry`（LobbyServer，例如 `CreateGameRoom`）。

## 约定

- handler 很薄：继承 `MessageHandler<TRequest>`，委托给 `LobbyManager` / `RoomManager`（LobbyServer）或 `GameRoomManager`（GameServer）。房间/玩家状态放在这些 manager 里，而不是 handler 里。
- 服务端注释和日志是中文，修改相邻代码时保持该风格。
- 玩家标识是 `long UserId`；`PlayerManager` 维护 `NetPeer → userId` 映射，并跟踪 `PlayerState`（InLobby/InRoom/Ready/InGame）。
- 房间流程：`RoomManager.PickGameServer` 根据每个 GameServer 周期性 `GameServerUpdate` 上报的 `GameServerInfo.PlayerCount` 做负载均衡。
- 房间类型：`Default` 房主开局、全员先准备，开局后 `IsStarted` 拒绝加入；`QuickMatch` 自由加入，任意玩家点开始只对本人开局。创建房间时 `CreateRoomRequest.MaxPlayers` 传入人数上限（0=无上限），LobbyServer 在 `JoinRoom` 时按此判断，满员返回 `RoomFull` 且 `IsStarted` 拒绝再加入（人数限制只在 LobbyRoom 层，GameRoom 不限制）。
