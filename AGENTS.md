# AGENTS.md

.NET 10（net10.0）游戏网络后端。三个可执行程序 + 一个共享库。无测试、无 CI、无 README。

## 构建与运行

```sh
dotnet build GameServer.sln        # 验证构建；预期 0 警告
dotnet run --project LobbyServer   # 必须先启动（监听 :6001）
dotnet run --project GameServer    # 启动时连接 LobbyServer 并注册自身
dotnet run --project TestClient    # 交互式命令行客户端（连接 :6001）
```

启动顺序有依赖：LobbyServer → GameServer → TestClient。GameServer 断线后每 5 秒自动重连 LobbyServer；在至少一个 GameServer 注册之前无法创建房间。

## 项目结构

- `SharedLib/` — 共享模型、`MessageIds`、`ReturnCode`、`MessageHelper`、配置加载。两个服务端和 TestClient 都引用它。网络相关依赖包都在这里（LiteNetLib、MessagePack、Serilog、Newtonsoft.Json）。
- `LobbyServer/` — 大厅与房间管理（入口：`LobbyServer/Program.cs` → `LobbyServer.cs`）。
- `GameServer/` — 游戏内房间处理（入口：`GameServer/Program.cs` → `GameServer.cs`）。
- `TestClient/` — CLI 手动测试客户端，可走完整大厅→房间→游戏流程。

配置文件 `LobbyServerConfig.json` / `GameServerConfig.json` 会被复制到输出目录（`CopyToOutputDirectory`）。双方 `ConnectionKey` 必须一致（`Game@wasd9527`）；客户端通过 `request.AcceptIfKey` 校验连接。

## 网络协议（新增消息时）

消息是 LiteNetLib UDP 二进制格式：`ushort messageId` + `byte returnCode` + MessagePack 序列化的 payload。用 `MessageHelper.Send(peer, messageId, code, data)` 组帧。

新增一条消息需要改四个地方：
1. 在 `SharedLib/Protocol/MessageIds.cs` 添加常量（客户端消息 <100，服务端内部消息 ≥100）。
2. 在 `SharedLib/Models/` 添加请求/响应模型。
3. 添加实现 `ILobbyHandler`（LobbyServer）或 `IGameHandler`（GameServer）的 handler，暴露 `MessageId` 和 `Handle(NetPeer peer, byte[] payload)`；用 `MessagePackSerializer.Deserialize<T>(payload)` 反序列化。
4. 在 `LobbyServer.cs` 的 `RegisterHandlers()` 或 `GameServer.cs` 的 `RegisterHandlers()` 中注册。

GameServer 维护两个注册表：`_gameRegistry`（来自游戏客户端）和 `_lobbyRegistry`（来自 LobbyServer，例如 `CreateGameRoom`）。

## 约定

- handler 很薄：委托给 `LobbyManager` / `RoomManager`（LobbyServer）或 `GameRoomManager`（GameServer）。房间/玩家状态放在这些 manager 里，而不是 handler 里。
- 服务端注释和日志是中文，修改相邻代码时保持该风格。
- 玩家标识是 `long UserId`；`PlayerManager` 维护 `NetPeer → userId` 映射，并跟踪 `PlayerState`（InLobby/InRoom/Ready/InGame）。
- 房间流程：`RoomManager.PickGameServer` 根据每个 GameServer 周期性 `GameServerUpdate` 上报的 `GameServerInfo.PlayerCount` 做负载均衡。
