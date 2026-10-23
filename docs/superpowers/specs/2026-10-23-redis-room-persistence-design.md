# Redis 房间持久化与重启恢复设计

日期：2026-10-23
状态：已确认设计，待实现

## 1. 背景与目标

当前 LobbyServer 的房间、成员、房主只存在内存中。只要 LobbyServer 重启，所有房间都会消失；GameServer 重启后，Lobby 也不知道要为它重新创建哪些游戏房间。

本设计引入 Redis 持久化：

1. **只归档 LobbyServer 的房间**（`LobbyRoom` + 成员 + 房主 + 绑定的 GameServer 地址）。
2. LobbyServer 重启后从 Redis 恢复房间和成员（成员初始为离线状态）。
3. GameServer 不归档房间；GameServer 重启后，由 LobbyServer 在它重新注册时按 `Address:Port` 重新下发 `CreateGameRoom`。
4. Lobby 和 GameServer 同时重启时：Lobby 先从 Redis 恢复，GameServer 注册后自动补建游戏房间。
5. Redis 只作为 Lobby 房间的持久化存储，不作为在线会话/`NetPeer` 的存储。

## 2. 非目标

- 不持久化 `NetPeer`、连接状态、在线状态；玩家重连后由 Lobby 重新绑定 peer。
- 不归档 GameServer 的 `GameRoom` 和游戏内成员。
- 不支持多个 LobbyServer 实例（当前假设单 Lobby）。
- 不做 GameServer 地址变化后的房间迁移；恢复依赖稳定的广告 `Address:Port`。
- 不做 Lobby 重连时的“关闭 GameServer 多余房间”对账（仅补建缺失房间）。
- 不设置房间 TTL；房间永久保留，直到显式关房/空房删除。

## 3. 总体设计

```text
                 ┌────────────────────────────┐
                 │ Redis                      │
                 │  {prefix}:lobby:rooms      │
                 │   Hash<roomId, JSON>       │
                 └─────────────▲──────────────┘
                               │ Save/Delete/Restore
LobbyServer ───────────────────┘
   │  CreateGameRoom (建房 / GS 重连补建)
   ▼
GameServer（不写 Redis）
```

恢复路径：

- **Lobby 重启**：`RestoreFromStore()` 读取 Hash → 重建 `LobbyRoom` + 离线 `LobbyPlayer`。
- **GameServer 重启**：GS 重新注册 → `GameServerRegisterHandler` 调用 `RoomManager.ResyncGameServer` → 对绑定到该 `Address:Port` 的每个 LobbyRoom 发 `CreateGameRoom`（幂等）。
- **两者都重启**：Lobby 先恢复，GS 注册后补建。

## 4. Redis 数据模型

只使用一个 Hash：

```text
{prefix}:lobby:rooms       Hash<roomId, JSON(LobbyRoomSnapshot)>
```

`prefix` 来自配置，默认 `gameserver`。

```csharp
public class LobbyRoomSnapshot
{
    public int Version { get; set; } = 1;
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int MaxPlayers { get; set; }
    public long OwnerUserId { get; set; }
    public long? CreatorUserId { get; set; }
    public string GameServerAddress { get; set; } = string.Empty;
    public int GameServerPort { get; set; }
    public List<PlayerInfo> Players { get; set; } = []; // 按加入顺序
}
```

- `Players` 顺序与 `LobbyRoom.PlayerIds` 一致，保证恢复后房主继承顺序不变。
- `PlayerInfo` 直接复用现有模型，包含昵称/头像等离线展示需要的信息。
- 不设 TTL；正常关房时 `HDEL`。
- 使用 Hash 而不是“每房一个 key + 索引 Set”：`HGETALL` 一次恢复全部，`HSET`/`HDEL` 原子，避免索引不一致。

## 5. Redis 工具与存储层

新增 NuGet：`StackExchange.Redis`（放在 `SharedLib`）。

### 5.1 RedisHelper

`SharedLib/Redis/RedisHelper.cs`：

```csharp
public sealed class RedisHelper : IDisposable
{
    public RedisHelper(string connectionString, int database = 0);
    public bool Ping();
    public T? HashGet<T>(string key, string field);
    public IReadOnlyDictionary<string, T> HashGetAll<T>(string key);
    public bool HashSet<T>(string key, string field, T value);
    public bool HashDelete(string key, string field);
    public bool Delete(string key);
    public IEnumerable<string> ScanKeys(string pattern);
    public void Dispose();
}
```

- 连接使用 `ConfigurationOptions`，`AbortOnConnectFail = true`；构造失败即抛异常。
- JSON 使用 Newtonsoft.Json（项目已有依赖），不新增序列化库。
- 单条记录反序列化失败时跳过并记录错误，不让一个坏房间阻止整个恢复。
- `ScanKeys` 仅用于测试清理/运维排查。

### 5.2 存储接口

`LobbyServer/Persistence/ILobbyRoomStore.cs`：

```csharp
public interface ILobbyRoomStore
{
    IReadOnlyList<LobbyRoomSnapshot> LoadAll();
    void Save(LobbyRoomSnapshot room);
    void Delete(string roomId);
}
```

实现：

- `RedisLobbyRoomStore`：生产实现，key 为 `{prefix}:lobby:rooms`。
- `InMemoryLobbyRoomStore`：测试实现，线程安全字典（放在测试项目）。

运行期 `Save`/`Delete` 抛出的 Redis 异常由 `RedisLobbyRoomStore` 捕获并记录 `Log.Error`，不影响内存状态（对应“启动失败、运行中降级”的策略）。

## 6. Lobby 启动恢复与离线成员

### 6.1 启动顺序

`LobbyServer.Start`：

1. 创建 `PlayerManager`、`RoomManager`（注入 `ILobbyRoomStore`）；
2. `RoomManager.RestoreFromStore()`；
3. 注册 handler；
4. 启动两个 NetManager。

恢复必须在开始接收连接之前完成。

停机时 `LobbyServer.Stop()` 会设置 `_stopping` 标志，并在 `OnClientDisconnected` / `OnServerDisconnected` 中直接返回：进程退出不能被误判为玩家主动离开，否则会把内存中的删除操作写回 Redis，导致重启恢复失效。

### 6.2 恢复规则

对每个快照：

- 创建 `LobbyRoom`，恢复 `RoomId/RoomType/MaxPlayers/OwnerUserId/CreatorUserId/GameServerAddress/GameServerPort`，`PlayerIds` 按快照顺序填充。
- 对每个成员调用 `PlayerManager.AddOffline(playerInfo, roomId)`，创建 `LobbyPlayer`：
  - `Peer = null`
  - `State = PlayerState.Offline`
  - `CurrentRoomId = roomId`
- 空房处理：
  - `Players.Count == 0 && CreatorUserId != null`：正常玩家房不应为空，视为脏数据，从 Redis 删除并跳过恢复。
  - `Players.Count == 0 && CreatorUserId == null`：系统房允许为空，保留。
- 房主校验：若 `OwnerUserId` 不在成员列表中且成员非空，房主改为第一个成员并记录警告。
- 同一个 userId 出现在多个房间：保留第一个，后续记录错误并跳过（不应发生）。

### 6.3 离线成员与重连

`LobbyPlayer.Peer` 改为 `NetPeer?`。

`PlayerManager` 新增/调整：

```csharp
LobbyPlayer AddOffline(PlayerInfo info, string roomId);
void BindOnline(LobbyPlayer player, NetPeer peer, PlayerInfo info);
void UnbindPeer(NetPeer peer);
void Remove(long userId);                  // 兼容 Peer == null
IEnumerable<NetPeer> OnlinePeers { get; }  // 仅 Connected 的 peer
bool IsOnline(long userId);                // Peer 非空且 Connected
```

`LobbyManager.Join` 规则：

| 现状 | 行为 |
|---|---|
| 不存在该 userId | 正常添加上线玩家 |
| 已存在且 `Peer == null`（恢复的离线成员） | 绑定新 peer，更新 `PlayerInfo`，**保留房间/房主身份**，保存快照 |
| 已存在且 `Peer == peer`（重复 JoinLobby） | 更新 `PlayerInfo`，保留房间，保存快照 |
| 已存在且 `Peer != peer`（顶号） | 解绑旧 peer、断开旧连接，绑定新 peer，**保留房间/房主身份**，保存快照 |

关键点：顶号/重连不再把玩家从房间移除；旧连接的断开事件因为 peer 映射已解绑，不会误删新会话。

运行期客户端断开也走 `PlayerManager.UnbindPeer(peer)`：只清 `Peer` 并置 `State = Offline`，保留房间成员/房主，不写 Redis。重连时 `BindOnline` 根据 `CurrentRoomId` 恢复 `InRoom` 或 `InLobby`。

### 6.4 广播与通知过滤

所有向玩家发送消息的位置必须过滤掉离线成员：

- `LobbyManager.Broadcast` 使用 `PlayerManager.OnlinePeers`。
- `RoomManager` 的 `JoinRoomNotify`、`LeaveRoomNotify`、`RoomOwnerChangedNotify`、`RoomClosedNotify` 在收集 peer 时跳过 `Peer == null` 或非 `Connected`。
- `RoomManager.BuildRoomInfo` 仍然通过 `PlayerManager` 取 `PlayerInfo`，离线成员也能出现在房间成员列表里。

## 7. 持久化写点

全部在 `RoomManager` 内完成，handler 不直接访问 Redis。

| 操作 | 动作 |
|---|---|
| `CreateRoom` 成功 | `Save(snapshot)` |
| `JoinRoom` 成功 | `Save` |
| `LeaveRoom`，房间仍未空 | `Save`（成员、房主可能变化） |
| `LeaveRoom`，房间变空并删除 | `Delete(roomId)` |
| `CloseRoom` | `Delete(roomId)` |
| `LobbyManager.Join` 重连/顶号且玩家在房间 | `Save`（`PlayerInfo` 可能更新） |
| 启动恢复 | 只读，不回写 |

`RoomManager` 现有公共方法已经用 `[MethodImpl(MethodImplOptions.Synchronized)]` 串行化；Redis 写同步执行在锁内。Redis 本机调用延迟低，先接受；如果后续要避免锁内网络 I/O，可改成“锁内生成快照、锁外入队写”。

## 8. GameServer 重启恢复

GameServer 不写 Redis。

`GameServerRegisterHandler` 在 `_registry.Register(peer, request)` 之后调用：

```csharp
_rooms.ResyncGameServer(peer, request);
```

`RoomManager.ResyncGameServer(NetPeer peer, GameServerInfo info)`：

1. 取得 `GameServerAddress == info.Address && GameServerPort == info.Port` 的所有房间；
2. 对每个房间发送 `CreateGameRoom { RoomId, RoomType, MaxPlayers }`（`TrySend`，失败记录日志）；
3. 记录本次补建数量。

行为：

- GS 没有重启：`CreateGameRoom` 幂等，保留现有房间/成员。
- GS 重启过：房间表为空，消息重新创建空房间；玩家重连后重新 `JoinGame`。
- Lobby 重启过：先从 Redis 恢复房间，GS 注册时再补建。
- 两者都重启：Redis 恢复 + 注册补建，闭环。

## 9. 配置与失败策略

`LobbyServerConfig` 新增：

```csharp
public string RedisConnection { get; set; } = "localhost:6379";
public string RedisKeyPrefix { get; set; } = "gameserver";
public int RedisDatabase { get; set; } = 0;
```

`LobbyServerConfig.json` 同步加入这三项。

`LobbyServer` 构造函数支持注入：

```csharp
public LobbyServer(LobbyServerConfig config, ILobbyRoomStore? store = null)
```

未注入时创建 `RedisLobbyRoomStore`。连接失败：

- 构造函数抛异常；
- `Program.cs` 捕获并 `Log.Fatal`，进程以非 0 退出；
- 不允许无 Redis 静默启动。

运行期写失败：

- `RedisLobbyRoomStore` 记录 `Log.Error`，内存状态继续；
- 重启后可能回退到 Redis 中的旧快照；
- 不阻塞游戏流程。

## 10. 并发与顺序

- Lobby 单实例，`RoomManager` 的同步锁保证房间状态和 Redis 写入顺序一致。
- Redis Hash 的单条 `HSET`/`HDEL` 原子；不需要事务。
- 恢复在 `PollEvents` 开始前完成，不存在并发恢复。
- GS 注册补建在 Lobby 的 server 接收线程执行，与 `RoomManager` 的锁互斥。

## 11. 测试计划

### 11.1 单元/组件测试（不依赖真实网络）

1. `RedisHelperTests`：Hash 读写/删除/Scan；使用唯一 prefix，结束时清理。
2. `LobbyRoomStoreTests`：快照保存、读取、删除；坏 JSON 被跳过。
3. `RoomManagerRestoreTests`：
   - 用 `InMemoryLobbyRoomStore` 保存玩家房（成员顺序、房主、GS 地址）；
   - 新建 `RoomManager` + `PlayerManager`，`RestoreFromStore`；
   - 断言房间、成员顺序、房主、离线状态；
   - 空玩家房被删除，空系统房被保留。

### 11.2 集成测试（真实服务端）

4. `LobbyRestart_RestoresRoomsAndMembers`：
   - 启动 Lobby（Redis 唯一 prefix）+ GS；
   - 玩家建房、另一玩家加入；
   - 停止 Lobby，用相同 prefix 启动新 Lobby；
   - 两个玩家重新 `JoinLobby`（相同 userId），验证房间仍在、房主/成员保留；
   - 验证 `JoinRoom` 不产生幽灵成员。
5. `GameServerRestart_RecreatesGameRooms`：
   - Lobby 建房，玩家进入 GameRoom；
   - 停止 GS，启动新 GS；
   - 等 GS 注册后，游戏客户端 `JoinGame` 成功。
6. 现有 27 个测试：`ServerFixture` 注入 `InMemoryLobbyRoomStore`，不依赖 Redis；Redis 测试单独使用真实 Redis 和唯一 prefix。

### 11.3 测试基建

- `LobbyServer.Stop()`：停止两个 NetManager，供重启测试和优雅停机使用。
- `ServerFixture` 增加停止/重启 Lobby、重启 GameServer 的能力。
- Redis 测试在 `Dispose` 中用 `ScanKeys(prefix + "*")` 清理 key。
- 真实 Redis 测试要求本机 `localhost:6379` 可用；项目无 CI，本地运行。

## 12. 影响文件

**SharedLib**

- `SharedLib.csproj`：新增 `StackExchange.Redis`
- 新增 `Redis/RedisHelper.cs`

**LobbyServer**

- `LobbyServerConfig.cs`、`LobbyServerConfig.json`
- 新增 `Persistence/LobbyRoomSnapshot.cs`、`Persistence/ILobbyRoomStore.cs`、`Persistence/RedisLobbyRoomStore.cs`
- `RoomManager.cs`（注入 store、恢复、保存、补建）
- `PlayerManager.cs`、`LobbyPlayer.cs`（离线成员、peer 可空、绑定/解绑）
- `LobbyManager.cs`（重连/顶号规则、广播过滤）
- `LobbyServer.cs`（store 注入、启动恢复、Stop、注册 handler）
- `Handlers/GameServerRegisterHandler.cs`（注册后补建）
- `Program.cs`（Redis 启动失败日志/退出码、停机调用 Stop）

**测试**

- `ServerFixture.cs`（注入 InMemory store、重启能力）
- 新增 `InMemoryLobbyRoomStore.cs`、`RedisHelperTests.cs`、`LobbyRoomStoreTests.cs`、`RoomManagerRestoreTests.cs`、`LobbyRestartTests.cs`、`GameServerRestartTests.cs`

**文档**

- `AGENTS.md`：Redis 配置、启动依赖、恢复语义
- `docs/superpowers/specs/2026-10-23-redis-room-persistence-design.md`（本文）

## 13. 已知限制

- Lobby 重启后，离线成员永久保留；没有 TTL，也不会自动清理长期不回来的玩家。
- 运行期 Redis 写失败会导致重启后回退到旧快照；没有补偿/全量重同步。
- GameServer 广告地址变化后，Lobby 恢复的房间匹配不到新地址；需要 ServerId 或地址更新协议。
- GS 重连只补建缺失房间，不关闭 GS 上多余的房间；如果 Lobby 在 GS 离线期间关过房，GS 侧可能残留。
- GameServer 离线时，恢复/已有的 LobbyRoom 仍会出现在房间列表并允许 `JoinRoom`；客户端拿到地址后可能连接失败，暂不增加“GameServer 离线”标记。
- 多 Lobby 实例不支持；同一 prefix 会被多个实例互相覆盖。
- 房间成员恢复为离线状态，`RoomInfo` 不包含在线标记；客户端无法区分成员是否在线。

## 14. 发布与兼容

- LobbyServer 启动前必须能连接 Redis；GameServer 无变化。
- 新增配置项有默认值，旧配置文件可直接使用。
- Redis key 使用 `{prefix}:lobby:rooms`，不同环境通过 prefix 隔离。
- 本次不改协议消息；`CreateGameRoom` 复用现有消息，仅增加“GS 注册后补建”的触发时机。
