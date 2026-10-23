# GameServer 设计文档

> **维护约定（必须遵守）**
>
> 任何功能改动或新增——包括协议消息、玩家/房间状态、Redis 结构、配置项、重连与停机语义、测试策略——都必须在**同一个提交**中同步更新本文件 `DESIGN.md`。
>
> 所有方法必须写注释：生产代码与测试中的方法（含 `private` / `static` / 局部函数、构造函数）使用中文 XML 文档注释；接口实现/重写可用 `<inheritdoc/>`。详见第 20 节。
>
> 文档描述的是当前实现，不是历史计划。代码与文档不一致时，先判断哪边是错的，再一起修正；不要只改代码。

---

## 1. 项目概述

这是一个基于 UDP 的多人在线游戏后端原型，使用 .NET 10 实现。系统由四个可执行程序和一个共享库组成：

- **LobbyServer**：大厅、组队房间、玩家在线状态、Redis 房间持久化。
- **GameServer**：游戏内房间与玩家、状态同步广播。
- **TestClient**：交互式命令行测试客户端。
- **SharedLib**：协议、模型、消息发送/解析、handler 抽象、配置、Redis 工具。

设计目标是：

1. 大厅房间是「组队房」，只负责把玩家组织到一起，并分配一台 GameServer。
2. 玩家加入大厅房间后，客户端自行连接 GameServer 并发 `JoinGame`。
3. 没有准备、开始、授权流程。
4. Lobby 的房间/成员/房主持久化到 Redis，Lobby 重启可恢复。
5. GameServer 不持久化房间；重启后由 Lobby 在注册时补建。

---

## 2. 技术栈

| 组件 | 版本/说明 |
|---|---|
| .NET | 10.0，`net10.0` |
| 网络 | LiteNetLib 2.1.4（UDP，可靠/顺序通道） |
| 序列化 | MessagePack 3.1.12（LZ4 BlockArray） |
| Redis 客户端 | StackExchange.Redis 3.4.5 |
| JSON | Newtonsoft.Json 13.0.4（配置与 Redis 快照） |
| 日志 | Serilog（Console + File） |

---

## 3. 总体架构

```text
                        ┌─────────────────────────────┐
                        │ Redis                       │
                        │  {prefix}:lobby:rooms       │
                        │  Hash<roomId, JSON snapshot>│
                        └──────────────▲──────────────┘
                                       │ 启动恢复 / 变更保存
                                       │
TestClient ──UDP:6001(ClientKey)──> LobbyServer ──UDP:6002(ServerKey)──> GameServer
   │                                   │                                   │
   │                                   ├─ PlayerManager (_players)         ├─ GameRoomManager
   │                                   ├─ RoomManager (LobbyRoom)          ├─ GamePlayerManager
   │                                   └─ GameServerRegistry               └─ 5s 负载上报
   │
   └──UDP:7001(ClientKey)──────────────────────────────────────────────────> GameServer
```

角色分工：

- **LobbyServer** 是房间元数据的唯一权威：房主、人数上限、绑定哪台 GameServer。
- **GameServer** 只管理游戏内成员，不保存房主，不持久化房间。
- **Redis** 只保存 LobbyRoom 快照；GameServer 不写 Redis。
- **客户端** 负责在大厅房间和游戏房间之间切换；服务端不做自动迁移。

---

## 4. 进程、端口与连接 key

| 服务 | 端口 | 连接 key | 用途 |
|---|---|---|---|
| LobbyServer | 6001（ClientPort） | `ClientConnectionKey` | 普通玩家 |
| LobbyServer | 6002（ServerPort） | `ServerConnectionKey` | GameServer 内部通信 |
| GameServer | 7001（Port） | `ClientConnectionKey` | 游戏客户端 |
| Redis | 6379 | `RedisConnection` | Lobby 房间持久化 |

连接 key 分角色，客户端 key 与 GameServer 内部 key 互不通用。

---

## 5. 项目结构

```text
SharedLib/
  Config/          配置模型与 ConfigLoader（相对路径解析到程序目录）
  Handlers/        IHandler / HandlerRegistry / MessageHandler<T> / LobbyStateHandler<T>
  Models/          协议模型、PlayerInfo、RoomInfo、GameServerInfo 等
  Protocol/        MessageIds、ReturnCode
  Redis/           RedisHelper
  Utils/           MessageHelper、MessageFrame、AddressResolver、PerformanceMonitor

LobbyServer/
  Cluster/         GameServerRegistry、ServerEntry
  Handlers/
    Client/        客户端消息 handler、LobbyStateHandler
    Server/        GameServer 注册/更新 handler
  Lobby/           LobbyManager（Create 初始化存储/RoomManager、Join/Leave/Chat）
  Persistence/     LobbyRoomSnapshot、ILobbyRoomStore、RedisLobbyRoomStore
  Player/          PlayerManager、LobbyPlayer、PlayerDisconnectPolicy
  Room/            RoomManager、LobbyRoom
  LobbyServer.cs   两端 NetManager、handler 注册、外部 API、Stop
  Program.cs       配置加载、RedisHelper 创建、启动、Ctrl+C、停机

GameServer/
  Handlers/        JoinGame/LeaveGame/同步消息、CreateGameRoom/CloseGameRoom
  Player/          GamePlayerManager、GamePlayer
  Room/            GameRoomManager、GameRoom
  GameServer.cs    客户端端口、Lobby 连接、5s 上报/重连
  Program.cs       配置加载、启动、Ctrl+C

TestClient/        交互式命令行客户端
```

---

## 6. 网络协议

### 6.1 帧格式

每条消息：

```text
ushort messageId + byte returnCode + MessagePack(payload, LZ4 BlockArray)
```

- 发送统一走 `MessageHelper`：
  - `CreateFrame(messageId, code, data)` 构建 `MessageFrame`
  - `Send(peer, frame, method)` / `Send(peer, messageId, code, data, method)`
  - `SendToAll(peers, frame, method)` 广播复用字节
  - `TrySend(peer, frame, method)` 连接不可用或异常时返回 `false`
- 接收统一走 `MessageHelper.ReadFrame(reader)` → `MessageFrame`，再用 `MessageHelper.Deserialize<T>(frame)`。
- 控制消息默认 `ReliableOrdered`；高频同步使用 `Sequenced`。

### 6.2 消息 ID

客户端消息 `< 100`，服务端内部消息 `>= 100`。

| ID | 名称 | 方向/用途 |
|---|---|---|
| 1 | `JoinLobby` | Client → Lobby；响应同 ID |
| 2 | `LeaveLobby` | Client → Lobby |
| 3 | `Chat` | Client → Lobby |
| 4 | `ChatNotify` | Lobby → Client 广播 |
| 10 | `CreateRoom` | Client → Lobby |
| 11 | `JoinRoom` | Client → Lobby |
| 12 | `LeaveRoom` | Client → Lobby |
| 13 | `JoinRoomNotify` | Lobby → 房间成员 |
| 14 | `LeaveRoomNotify` | Lobby → 房间成员 |
| 15 | `RoomList` | Client → Lobby |
| 16 | `RoomOwnerChangedNotify` | Lobby → 房间成员 |
| 17 | `RoomClosedNotify` | Lobby/GameServer → 客户端 |
| 30 | `JoinGame` | Client → GameServer |
| 31 | `LeaveGame` | Client → GameServer |
| 32 | `JoinGameNotify` | GameServer → 房间成员 |
| 33 | `LeaveGameNotify` | GameServer → 房间成员 |
| 40 | `PositionSync` | 游戏内同步 |
| 41 | `AnimationSync` | 游戏内同步 |
| 42 | `ObjectSpawn` | 游戏内同步 |
| 43 | `ObjectDespawn` | 游戏内同步 |
| 100 | `GameServerRegister` | GameServer → Lobby |
| 101 | `GameServerUpdate` | GameServer → Lobby（5s） |
| 102 | `CreateGameRoom` | Lobby → GameServer |
| 103 | `CloseGameRoom` | Lobby → GameServer |

### 6.3 ReturnCode

| 值 | 名称 | 使用场景 |
|---|---|---|
| 0 | `Success` | 成功 |
| 1 | `Error` | 通用失败（如 RoomId 冲突） |
| 2 | `RoomNotFound` | 房间不存在 |
| 3 | `AlreadyInRoom` | GameServer：peer/user 已在游戏房 |
| 4 | `NoGameServerAvailable` | 建房时没有可用 GameServer |
| 5 | `NotInLobby` | 玩家未加入大厅 |
| 6 | `NotInRoom` | 玩家不在房间 |
| 7 | `NotRoomOwner` | 保留（旧开局流程） |
| 8 | `NotAllReady` | 保留（旧准备流程） |
| 9 | `DeserializeFailed` | 请求反序列化失败 |
| 10 | `RoomFull` | 房间人数达到上限 |
| 11 | `GameServerOffline` | `CreateGameRoom` 发送失败 |
| 12 | `NotAuthorized` | 保留（旧授权流程） |

### 6.4 Handler 抽象

- `IHandler`：`MessageId` + `Handle(peer, messageId, payload)`。
- `MessageHandler<TRequest>`：负责反序列化、`TryAuthorize`/`OnUnauthorized`、`OnDeserializeFailed`，子类只实现 `HandleMessage(peer, request)`。
- `LobbyStateHandler<TRequest>`：要求 peer 已绑定 userId，否则返回 `NotInLobby`。
- `HandlerRegistry`：按 messageId 分发；未知消息只记录日志，**不回发 Error**，避免 Lobby↔GameServer 互相回显形成死循环。
- 新增消息需要改四处：`MessageIds`、模型、handler、对应 `RegisterHandlers()`；同时更新本文件第 6 节。

---

## 7. 大厅玩家模型

### 7.1 数据结构

`PlayerManager` **只维护一张表**：

```csharp
ConcurrentDictionary<long, LobbyPlayer> _players;   // userId -> LobbyPlayer
```

`LobbyPlayer`：

```csharp
public class LobbyPlayer
{
    public PlayerInfo Info { get; set; }       // 昵称、头像等
    public NetPeer? Peer { get; set; }         // null = 当前离线
    public string? CurrentRoomId { get; set; } // null = 不在房间
}
```

- **没有** `NetPeer → userId` 反向字典。
- **没有** `PlayerState` 枚举。
- 在线状态 = `Peer != null` 且连接已连接。
- 所在房间 = `CurrentRoomId`。
- `GetByPeer(peer)` / `GetUserId(peer)` 线性扫描 `_players.Values`，O(n)。房间操作和鉴权会用到。

### 7.2 连接与加入大厅

```text
OnClientConnected(peer)         只打印日志，不写 _players
收到 JoinLobby
  ├─ userId 已存在
  │    ├─ Peer == null（离线/Redis 恢复）→ BindOnline，保留房间和 CurrentRoomId
  │    ├─ Peer == 同一个 peer         → 更新 Info，保留房间
  │    └─ Peer != 新 peer（顶号）     → 解绑并断开旧 peer，绑定新 peer，保留房间
  └─ userId 不存在 → _players.Add(peer, PlayerInfo)
```

- 同一连接切换 userId：先 `RoomManager.RemovePlayer(oldUserId)`，再处理新 userId。
- 重连时如果玩家在房间中，`PersistPlayerRoom(userId)` 更新 Redis 里的 PlayerInfo。
- `JoinLobbyResponse` 只返回 `PlayerInfo`，不返回当前房间；客户端需要再发 `JoinRoom` 获取 `RoomInfo`。已在房间的成员调用 `JoinRoom` 返回 `Success + RoomInfo`，不会返回 `AlreadyInRoom`。

### 7.3 主动退出与断线策略

| 事件 | 判定 | 处理 |
|---|---|---|
| `LeaveLobby` | 主动退出 | `RoomManager.RemovePlayer(userId)`：离开房间、删除 LobbyPlayer、更新/删除 Redis |
| 客户端关闭连接 | `RemoteConnectionClose` | 同上，移除玩家和房间成员 |
| 服务端主动断开 | `DisconnectPeerCalled` | 同上；顶号时旧 peer 已先解绑，因此不会再误删新会话 |
| 异常断线 | `Timeout`、`ConnectionFailed`、`HostUnreachable`、`NetworkUnreachable`、`ConnectionRejected`、`InvalidProtocol`、`UnknownHost`、`Reconnect`、`PeerToPeerConnection`、`PeerNotFound` | 只清 `Peer`，保留 LobbyPlayer、房间成员、房主、`CurrentRoomId`；不写 Redis |

判定集中在 `PlayerDisconnectPolicy.ShouldKeepPlayer(reason)`。

异常断线后玩家重连：

1. 新 peer 发 `JoinLobby`，找到已有离线 LobbyPlayer；
2. `BindOnline` 替换 Peer、更新 Info；
3. 房间/房主/`CurrentRoomId` 保持不变；
4. 发 `JoinRoom` 直接拿回 `RoomInfo`。

### 7.4 在线人数

- `PlayerManager.Count`：`_players` 总数，**包含离线玩家**。
- `PlayerManager.OnlineCount`：`Peer` 已连接的玩家数。
- 日志和 `LobbyManager.UserCount` 使用 `OnlineCount`。
- `RoomManager` 的广播/通知只发给 `OnlinePeers` 或 `Peer` 已连接的成员。

---

## 8. 大厅房间生命周期

### 8.1 LobbyRoom

```csharp
public class LobbyRoom
{
    public string RoomId;
    public RoomType RoomType;        // 当前只有 Default
    public int MaxPlayers;           // 0 = 无上限
    public List<long> PlayerIds;     // 按加入顺序，决定房主继承
    public long OwnerUserId;         // 0 = 无主（系统房尚未有人加入）
    public long? CreatorUserId;      // null = 外部/系统创建
    public string GameServerAddress;
    public int GameServerPort;
}
```

房间**不保存 PlayerInfo**；成员信息通过 `PlayerManager.Get(userId).Info` 获取。

### 8.2 创建房间

统一入口：

```csharp
RoomManager.CreateRoom(CreateRoomRequest request, long? creatorUserId)
```

玩家创建：

1. 校验玩家在 `_players`；
2. RoomId 生成/冲突校验；
3. `PickGameServer()` 选 `PlayerCount` 最少的在线 GameServer；
4. 发送 `CreateGameRoom`（`TrySend` 失败返回 `GameServerOffline`）；
5. 如果玩家已在其他房间，先 `LeaveRoom`；
6. 创建房间，成员 `[creator]`，房主 = creator；
7. `_players.SetRoom(userId, roomId)`；
8. 保存 Redis 快照。

系统/外部创建：

```csharp
LobbyServer.CreateSystemRoom(CreateRoomRequest request)
```

- `creatorUserId = null`；
- 房间为空，`OwnerUserId = 0`；
- 第一个 `JoinRoom` 的玩家成为房主；
- 通过服务端内部 API 调用，不新增网络入口。

没有可用 GameServer 返回 `NoGameServerAvailable`。

### 8.3 加入房间

1. 房间不存在 → `RoomNotFound`；
2. 已是成员 → `Success + RoomInfo`（幂等重进，供重连使用）；
3. 人数达到 `MaxPlayers` → `RoomFull`；
4. 玩家在别的房间 → 先 `LeaveRoom`；
5. 加入 `PlayerIds`；如果 `OwnerUserId == 0` 设为首个加入者；
6. `SetRoom`；
7. 向在线成员广播 `JoinRoomNotify`；
8. 保存 Redis 快照。

### 8.4 离开房间

1. 从 `PlayerIds` 移除，`ClearRoom`；
2. 房间空：
   - 删除房间；
   - Redis 删除快照；
   - 发 `CloseGameRoom(Empty)`；
3. 房间非空且离开者是房主：
   - 房主 = `PlayerIds[0]`（下一个加入者）；
   - 广播 `RoomOwnerChangedNotify`；
4. 广播 `LeaveRoomNotify`；
5. 保存 Redis 快照。

### 8.5 外部关闭房间

```csharp
LobbyServer.CloseRoom(roomId)  // RoomCloseReason.ClosedBySystem
```

- 删除大厅房间和 Redis 快照；
- 向在线大厅成员发 `RoomClosedNotify`；
- 清空所有成员的 `CurrentRoomId`；
- 发 `CloseGameRoom`，GameServer 通知并移出游戏内玩家（保留连接）。

### 8.6 房间列表

`RoomList` 返回所有房间的 `RoomId`、`RoomType`、`PlayerCount`。`PlayerCount` 是 `PlayerIds.Count`，**包含离线成员**。当前不返回在线人数或 GameServer 在线状态。

### 8.7 大厅房间不变量

- `LobbyRoom.PlayerIds` 中的每个 userId 都应该在 `PlayerManager._players` 中存在（在线或离线）。
- 房间成员变化、房主变化、房间关闭都必须同步 Redis。
- 玩家主动断开/离开时移除成员；异常断线时保留成员。
- 房间为空只在显式离开/关闭时发生；异常断线不会让房间变空。

---

## 9. 游戏房间生命周期

### 9.1 GameRoom

```csharp
public class GameRoom
{
    public string RoomId;
    public RoomType RoomType;
    public int MaxPlayers;
    public HashSet<long> PlayerIds;
}
```

- 不保存房主、不持久化。
- `MaxPlayers` 只由 Lobby 在 `JoinRoom` 时限制；GameServer 不重复校验。

### 9.2 创建

- Lobby 建房时发 `CreateGameRoom`。
- GameServer `TryAdd` **幂等**：房间已存在就保留现有成员，不清空。
- GameServer 重启后房间表为空；Lobby 在 GS 注册时补发。

### 9.3 JoinGame

1. 房间不存在 → `RoomNotFound`；
2. 该 peer 已在任何游戏房 → `AlreadyInRoom`；
3. 该 userId 已在目标房 → `AlreadyInRoom`；
4. 添加玩家，广播 `JoinGameNotify`；
5. 返回 `JoinGameResponse { RoomId, RoomType, Players }`。

没有授权校验；知道 RoomId 的客户端可以加入。

### 9.4 LeaveGame / 断线

- 移除玩家，广播 `LeaveGameNotify`。
- 房间空了**不删除**：生命周期由 Lobby 的 `CloseGameRoom` 决定。
- 这样玩家退出游戏后仍可重新 `JoinGame`。

### 9.5 CloseGameRoom

- 删除 GameRoom；
- 对仍在房间内的玩家发 `RoomClosedNotify`；
- 从 `GamePlayerManager` 移除玩家；
- **不主动断开** NetPeer，连接保留。

---

## 10. GameServer 注册、负载与重连补建

### 10.1 注册

GameServer 连接 Lobby 成功后发 `GameServerRegister`：

```csharp
new GameServerInfo {
    Address = PublicAddress 或自动探测本机 IPv4,
    Port = config.Port,
    PlayerCount = 0,
    RoomCount = 0
}
```

Lobby 的 `GameServerRegistry` 以 `Address:Port` 为唯一 key。

### 10.2 周期上报

GameServer 每 5 秒发 `GameServerUpdate`：

- `PlayerCount`、`RoomCount`
- `CpuPercent`、`MemoryMB`

Lobby 的 `PickLeastLoaded()` 选择在线且 `PlayerCount` 最少的 GameServer。

### 10.3 断线与重连

- Lobby 收到 GameServer 断开：只移除 registry 条目，房间保留。
- GameServer 每 5 秒检测 Lobby 连接，断开则重连。
- 重连注册成功后，Lobby 调用：

```csharp
RoomManager.ResyncGameServer(peer, info)
```

对绑定到该 `Address:Port` 的每个 LobbyRoom 重发 `CreateGameRoom`：

- GS 没重启：幂等，保留成员；
- GS 重启过：重新创建空房间，玩家重新 `JoinGame`；
- Lobby 重启过：先从 Redis 恢复房间，再补建；
- 两者都重启：Redis 恢复 + 注册补建。

### 10.4 广告地址

`GameServerConfig.PublicAddress` 为空时，`AddressResolver` 取本机第一个非回环 IPv4，兜底 `127.0.0.1`。该地址会写入 `GameServerInfo.Address`，也会出现在 `RoomInfo.GameServerAddress` 中。

地址变化后，Lobby 中已有房间仍指向旧地址；当前不做迁移（见第 17 节）。

---

## 11. Redis 房间持久化

### 11.1 范围

- **只持久化 Lobby 房间**：`LobbyRoom` + 成员 `PlayerInfo` + 房主 + 绑定 GameServer 地址。
- GameServer 不写 Redis。
- 运行期不在线状态、`NetPeer` 不持久化。

### 11.2 Key 与快照

```text
{RedisKeyPrefix}:lobby:rooms    Redis Hash
  field = roomId
  value = JSON(LobbyRoomSnapshot)
```

```csharp
public class LobbyRoomSnapshot
{
    public int Version = 1;
    public string RoomId;
    public RoomType RoomType;
    public int MaxPlayers;
    public long OwnerUserId;
    public long? CreatorUserId;
    public string GameServerAddress;
    public int GameServerPort;
    public List<PlayerInfo> Players;   // 按加入顺序
}
```

- 默认 prefix `gameserver`，默认 database 0。
- 不设 TTL；关房/空房时显式 `HDEL`。
- 使用 Hash 而非“每房一个 key + 索引 Set”：一次 `HGETALL` 恢复全部，`HSET`/`HDEL` 原子。

### 11.3 RedisHelper

`SharedLib/Redis/RedisHelper.cs`：

- 连接：`ConnectionMultiplexer`，`AbortOnConnectFail = true`，`ConnectTimeout = 5000`，`SyncTimeout = 1000`；`Ping` / `IsConnected`。
- String：`StringGet<T>` / `StringSet<T>` / `StringIncrement`。
- Key：`KeyExists` / `KeyExpire` / `KeyTimeToLive` / `Delete`。
- Hash：`HashGet<T>` / `HashGetAll<T>` / `HashSet<T>` / `HashSetAll<T>` / `HashDelete` / `HashExists` / `HashLength` / `HashKeys` / `HashIncrement`。
- List：`ListLeftPush<T>` / `ListRightPush<T>` / `ListRange<T>` / `ListLength` / `ListRemove<T>`。
- Set：`SetAdd<T>` / `SetRemove<T>` / `SetContains<T>` / `SetMembers<T>` / `SetLength`。
- 运维/测试：`ScanKeys`、`Database`。
- 实例应作为进程级单例由组合根创建并注入，避免每次请求新建连接；不建议做成静态全局单例。
- JSON 使用 Newtonsoft；单条记录反序列化失败会跳过并记录警告。

### 11.4 存储层

```csharp
public interface ILobbyRoomStore
{
    IReadOnlyList<LobbyRoomSnapshot> LoadAll();
    void Save(LobbyRoomSnapshot room);
    void Delete(string roomId);
}
```

- 生产实现：`RedisLobbyRoomStore`，使用注入的 `RedisHelper`，不拥有 Redis 连接。
- `LoadAll` 校验：
  - `Version == 1`；
  - `GameServerAddress` 非空、`GameServerPort > 0`；
  - Redis Hash field 作为 `RoomId` 的唯一可信来源；
  - 无效条目直接从 Redis 删除。
- 运行期 `Save`/`Delete` 捕获异常并记录日志，不抛出；内存状态继续。

### 11.5 写点

| 操作 | Redis |
|---|---|
| `CreateRoom` 成功 | `Save` |
| `JoinRoom` 成功 | `Save` |
| `LeaveRoom` 后房间非空 | `Save`（成员/房主变化） |
| `LeaveRoom` 后房间为空 | `Delete` |
| `CloseRoom` | `Delete` |
| 重连/顶号绑定并更新 Info | `PersistPlayerRoom` → `Save` |
| 异常断线 | **不写**（房间成员未变化） |
| 主动断开 | 经过 `RemovePlayer` → `LeaveRoom` → `Save`/`Delete` |

`RoomManager` 的公共方法使用 `[MethodImpl(MethodImplOptions.Synchronized)]`，Redis 写同步发生在锁内。`SyncTimeout = 1000ms` 是 Redis 故障时的最坏阻塞上限。

### 11.6 启动恢复

`LobbyManager.Create`（由 `LobbyServer.Start` 调用）：

1. 用注入的 `RedisHelper` 创建 `RedisLobbyRoomStore`；
2. 创建 `RoomManager`（注入 store）；
3. `RoomManager.RestoreFromStore()`；
4. 返回已恢复房间的 `LobbyManager`。

`LobbyServer.Start` 随后从 `LobbyManager.Rooms` 取得 `RoomManager`，注册 handler，启动 NetManager。

恢复规则：

- 每个成员创建离线 `LobbyPlayer`：`Peer = null`、`CurrentRoomId = roomId`；
- `PlayerIds` 顺序来自快照 `Players` 顺序；
- 空玩家房（`Players.Count == 0 && CreatorUserId != null`）视为脏数据，删除并跳过；
- 空系统房（`CreatorUserId == null`）保留；
- 房主不在成员列表时回退到第一个成员；
- 重复 userId 只保留第一个，后续跳过；
- 恢复完成后等待 GameServer 重连注册补建。

### 11.7 失败与停机

- `LobbyServer/Program.cs` 只创建 `RedisHelper`（组合根）；连接失败记录 Fatal 并以非 0 退出。
- `LobbyServer` 构造函数接收 `RedisHelper`，不创建 store、不连接 Redis；`Stop()` 只停止两个 NetManager。
- `LobbyManager.Create` 在初始化时创建 `RedisLobbyRoomStore` 和 `RoomManager`，并用注入的 `RedisHelper` 完成 Redis 访问。
- `Program` 在 `finally` 中先 `lobbyServer.Stop()`，再 `redisHelper.Dispose()`；`RedisLobbyRoomStore` 不拥有连接，不负责释放。
- `LobbyServer.Stop()` 设置 `_stopping`：停机期间忽略客户端/GameServer 断开事件，避免把进程退出误判为玩家离开并删除 Redis 房间。
- `Stop()` 是终止性的；需要重启请创建新的 `LobbyServer` 实例。
- 运行期写失败：记录 `Log.Error`，内存继续；重启后可能回退到旧快照。

---

## 12. TestClient

- 交互式命令行客户端，连接 LobbyServer `6001`。
- 命令：`joinlobby | leavelobby | chat <内容> | createroom [maxPlayers] [roomId] | joinroom <roomId> | leaveroom | rooms | joingame <roomId> | leavegame | status | quit`。
- `CreateRoom` / `JoinRoom` 成功后**不会自动进游戏**：
  - 记录 `roomId → GameServer Address/Port`；
  - 提示执行 `joingame <roomId>`。
- `joingame`：
  - 未连接 → 连接对应 GameServer；
  - 已连接同一 GS → 直接 `JoinGame`；
  - 目标在另一台 GS → 断开旧连接再连新的；
  - 同一连接切房 → 先 `LeaveGame`；
  - `RoomNotFound` 时 1 秒内重试。
- 处理 `RoomOwnerChangedNotify`、`RoomClosedNotify`。
- 没有准备/开始命令。

---

## 13. 线程模型与并发

- LobbyServer：
  - 两个 `NetManager`（客户端口/服务端口），由 `Program` 的 `PeriodicTimer`（`UpdateTime=15ms`）驱动 `PollEvents`。
  - `RoomManager` 公共方法同步（锁 `this`）；房间字典 `Dictionary`、成员 `List/HashSet` 在锁内修改。
  - `PlayerManager._players` 是 `ConcurrentDictionary`；`LobbyPlayer` 字段由 poll 线程/加锁的 RoomManager 修改。
  - Redis 写同步在 `RoomManager` 锁内，`SyncTimeout=1000ms`。
- GameServer：
  - 两个 `NetManager`（客户端口/Lobby 连接），主循环 `PollEvents`。
  - `GameRoomManager`/`GamePlayerManager` 由 poll 线程访问；`_rooms` 为 `ConcurrentDictionary`。
  - 独立的 5 秒 `UpdateLoop` 负责上报/重连；与 poll 线程共享 `PlayerCount` 等只读统计。

---

## 14. 配置

### 14.1 LobbyServerConfig

| 字段 | 默认值 | 说明 |
|---|---|---|
| `ClientPort` | 6001 | 玩家入口 |
| `ServerPort` | 6002 | GameServer 入口 |
| `ClientConnectionKey` | `Client@wasd9527` | 玩家连接 key |
| `ServerConnectionKey` | `Game@wasd9527` | GameServer 连接 key |
| `UpdateTime` | 15 | NetManager 轮询间隔 ms |
| `PingInterval` | 1000 | 心跳间隔 ms |
| `DisconnectTimeout` | 5000 | 断线超时 ms |
| `ChannelsCount` | 1 | LiteNetLib 通道数 |
| `RedisConnection` | `localhost:6379` | Redis 连接 |
| `RedisKeyPrefix` | `gameserver` | key 前缀 |
| `RedisDatabase` | 0 | Redis DB |

### 14.2 GameServerConfig

| 字段 | 默认值 | 说明 |
|---|---|---|
| `Port` | 7001 | 游戏客户端入口 |
| `LobbyAddress` | `127.0.0.1` | Lobby 地址 |
| `LobbyPort` | 6002 | Lobby 服务端口 |
| `LobbyConnectionKey` | `Game@wasd9527` | 连 Lobby 的 key |
| `ClientConnectionKey` | `Client@wasd9527` | 游戏客户端 key |
| `PublicAddress` | null | 对外广告地址；空则自动探测 |
| `UpdateTime` | 15 | 轮询间隔 ms |
| `PingInterval` | 1000 | 心跳间隔 ms |
| `DisconnectTimeout` | 5000 | 断线超时 ms |
| `ChannelsCount` | 1 | 通道数 |

配置文件由 `CopyToOutputDirectory` 复制到输出目录；`ConfigLoader` 将相对路径解析到 `AppContext.BaseDirectory`，因此 `dotnet run --project` 从仓库根启动也能加载输出目录里的配置。

---

## 15. 构建、运行与验证

### 15.1 前置条件

- .NET 10 SDK。
- 本机 Redis（默认 `localhost:6379`）；LobbyServer 启动前必须可连接。

### 15.2 命令

```sh
redis-cli ping                        # 期望 PONG
dotnet build GameServer.sln           # 预期 0 警告 0 错误
dotnet run --project LobbyServer      # 先启动
dotnet run --project GameServer       # 再启动
dotnet run --project TestClient       # 连接 :6001
```

启动顺序：**Redis → LobbyServer → GameServer → TestClient**。

### 15.3 验证方式

- 当前处于快速开发阶段，不维护自动化测试项目（原 `GameServer.Tests` 已删除）。
- 提交前验证：`dotnet build GameServer.sln` 必须 0 警告 0 错误。
- 功能验证：启动 Redis、LobbyServer、GameServer，再用一个或多个 TestClient 手动走大厅/房间/游戏流程。
- 后续需要回归保障时，再重新引入测试项目；在此之前，`DESIGN.md` 第 19 节的检查清单和 code review 是主要质量门禁。

---

## 16. 关键流程时序

### 16.1 玩家建房并进入游戏

```text
Client → Lobby: JoinLobby
Lobby: _players.Add / BindOnline
Client → Lobby: CreateRoom
Lobby: PickGameServer → CreateGameRoom → GameServer
Lobby: 创建 LobbyRoom，房主=creator，保存 Redis
Lobby → Client: CreateRoomResponse(RoomInfo，含 GS 地址)
Client → GameServer: JoinGame
GameServer: 校验房间/peer/user → 添加成员 → 广播 JoinGameNotify
```

### 16.2 异常断线重连

```text
Client 网络中断
Lobby: DisconnectReason=Timeout → PlayerDisconnectPolicy.ShouldKeepPlayer=true
Lobby: PlayerManager.UnbindPeer → Peer=null，保留 LobbyPlayer/房间/房主
Client 重连 → JoinLobby（相同 userId）
Lobby: 找到离线 LobbyPlayer → BindOnline → PersistPlayerRoom
Client → Lobby: JoinRoom → Success + RoomInfo（已在房间也返回 Success）
Client → GameServer: JoinGame
```

### 16.3 Lobby 重启

```text
Lobby 停止（Stop 设置 _stopping，不处理断开事件）
新 Lobby 启动 → Redis HGETALL
Lobby: 重建 LobbyRoom + 离线 LobbyPlayer（Peer=null）
GameServer 重连注册 → ResyncGameServer → CreateGameRoom（幂等）
玩家重连 JoinLobby → BindOnline → JoinRoom
```

### 16.4 GameServer 重启

```text
GameServer 进程重启 → 房间表为空
GameServer 连接 Lobby → GameServerRegister
Lobby: GameServerRegistry.Register → ResyncGameServer
Lobby → GameServer: 每个绑定该 Address:Port 的 LobbyRoom 发 CreateGameRoom
GameServer: TryAdd 空房间
玩家重新 JoinGame
```

---

## 17. 已知限制与非目标

- **单 LobbyServer**：不支持多 Lobby 实例；同一 Redis prefix 会被多个实例互相覆盖，没有分布式锁。
- **无登录鉴权**：`JoinLobby` 接受任意 `UserId`，知道 `RoomId` 的客户端可以 `JoinGame`；离线玩家身份可被顶替。
- **GameServer 地址变化不迁移**：LobbyRoom 以 `Address:Port` 关联 GameServer；GS 换地址后旧房间无法匹配。
- **只补建、不清理**：GS 重连时 Lobby 只补建缺失房间，不关闭 GS 上多余的房间。
- **离线玩家/房间永久保留**：没有 TTL/离线超时；离线成员占用 `MaxPlayers`，房间不会因异常断线变空。
- **协议不含在线状态**：`RoomInfo.Players` 只有 `PlayerInfo`；`RoomListInfo.PlayerCount` 包含离线成员。
- **运行期 Redis 写失败**：记录日志，内存继续；重启后可能回退到旧快照，没有异步补偿队列。
- **Redis 写锁内同步**：Redis 故障时单次房间操作最多阻塞 `SyncTimeout=1000ms`。
- **无协议版本协商**：客户端/两个服务端必须锁步发布。
- **`RoomType` 只有 `Default`**：字段保留，当前不承载分支逻辑。
- **日志文件路径**：Serilog 文件 sink 仍按进程 CWD 解析；配置路径已固定到程序目录。

---

## 18. 变更记录

| 日期 | 变更 |
|---|---|
| 2026-10-22 | 房间生命周期改造：删除准备/开始/授权；大厅房改为组队房；建房即同步 GameRoom；房主按加入顺序继承 |
| 2026-10-23 | Redis 房间持久化：Lobby 启动恢复、GameServer 注册补建、停机保护、消息 ID 重排（`CreateGameRoom=102`、`CloseGameRoom=103`） |
| 2026-10-23 | 配置路径修复；GameServer 重连注册误删修复 |
| 2026-10-23 | 玩家模型简化：移除 `_peerToUserId`，只保留 `_players`；移除 `PlayerState`；主动断开移除、异常断线保留 |
| 2026-10-23 | TestClient 改为手动 `joingame`；`RoomType` 只保留 `Default` |
| 2026-10-23 | 增加文档维护约定与“所有方法必须写注释”规范（第 20 节） |
| 2026-10-23 | LobbyServer Handler 目录按客户端/服务端拆分（`Handlers/Client`、`Handlers/Server`） |
| 2026-10-23 | Redis 连接与存储创建/释放从 `LobbyServer` 移到 `Program.cs` 组合根 |
| 2026-10-23 | RedisHelper 增加 String/Key/Hash/List/Set 常用接口并补全 XML 注释 |
| 2026-10-23 | 测试端口改为 UDP 预留；LobbyServer/GameServer 启动校验端口绑定，修复集成测试间歇性串端口 |
| 2026-10-23 | 删除 `GameServer.Tests`；快速开发阶段不维护自动化测试项目 |
| 2026-10-23 | `Program` 只创建 RedisHelper；`RedisLobbyRoomStore` 移到 `LobbyManager.Create` 初始化 |

---

## 19. 文档维护检查清单

功能改动/新增提交前逐项确认：

- [ ] 第 6 节：是否新增/删除消息 ID、模型或 ReturnCode？
- [ ] 第 7 节：玩家在线/离线、重连、踢号语义是否变化？
- [ ] 第 8 节：LobbyRoom 字段、房主规则、关房流程是否变化？
- [ ] 第 9 节：GameRoom 创建/加入/离开/关闭语义是否变化？
- [ ] 第 10 节：GameServer 注册、负载均衡、重连补建是否变化？
- [ ] 第 11 节：Redis key、快照字段、写点、恢复规则是否变化？
- [ ] 第 13 节：线程/锁/Redis 阻塞语义是否变化？
- [ ] 第 14 节：配置项是否有新增/默认值变化？
- [ ] 第 15 节：构建/运行/验证方式是否变化？
- [ ] 第 17 节：是否新增/消除已知限制？
- [ ] 所有新增/修改的方法是否都有中文 XML 注释（含私有方法、局部函数、测试方法）？
- [ ] 第 18 节：追加变更记录。
- [ ] `AGENTS.md` 是否也需要同步（构建、运行、协议约定、维护规则）。

---

## 20. 代码注释规范

### 20.1 强制规则

**所有方法都必须有注释**，包括：

- `public` / `private` / `protected` / `internal` 方法；
- `static` 方法；
- 构造函数；
- 局部函数（local function）；
- 测试方法、测试辅助方法；
- 接口方法的实现/重写。

### 20.2 格式

使用中文 XML 文档注释：

```csharp
/// <summary>
/// 根据 userId 从 Redis 恢复大厅房间和离线成员。
/// </summary>
/// <param name="userId">要查询的玩家 ID。</param>
/// <returns>恢复成功返回 true；玩家不存在返回 false。</returns>
public bool RestorePlayer(long userId)
{
    // ...
}
```

- `<summary>`：一句话说明“这个方法做什么”，不要只重复方法名。
- `<param>`：参数含义不直观时补充。
- `<returns>`：非 void 且返回值语义不直观时补充。
- 接口实现/重写：优先使用 `/// <inheritdoc/>`，避免重复接口注释。
- 复杂逻辑可在方法内部加行内注释，解释“为什么”，而不是逐行翻译代码。

### 20.3 执行方式

- 新增或修改方法时，注释必须与代码在同一个提交中完成。
- Code review / 自检时，方法没有注释视为未完成。
- 当前没有编译器强制（未启用 CS1591 等 analyzer），依靠 `DESIGN.md` 第 19 节检查清单和 review 保证。
- 历史方法在后续被修改时补注释；如需一次性补齐存量方法，单独提交，避免和功能改动混在一起。
