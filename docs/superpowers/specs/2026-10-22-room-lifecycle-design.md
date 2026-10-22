# 大厅房间与游戏房间生命周期改造设计

日期：2026-10-22
状态：已确认设计，待实现

## 1. 背景与目标

当前大厅房间承担了「准备 → 开局 → 授权」的职责，游戏房间直到 `StartGame` 才被创建，导致：

- 大厅房间与游戏房间生命周期不一致，游戏房间创建晚于玩家可见的房间；
- 满员复用 `IsStarted`、重复开局覆盖 `GameRoom` 等状态机问题；
- 房主固定为创建者，无法支持外部/系统建房；
- 准备/取消准备与开局逻辑对「组队房」场景来说过重。

本次改造把大厅房间定位为**组队房**：

1. 大厅房间支持创建、加入、离开（不再有准备、开始、授权）。
2. 大厅建房时**立即**通过内部消息让 GameServer 创建对应 `GameRoom`。
3. 创建者可以是玩家，也可以是服务端内部 API（供外部系统调用）。
4. 玩家创建时自动入房并成为房主；系统创建的房间为空房，**第一个加入者成为房主**。
5. 房主离开时按加入顺序转移给下一个成员，并通知客户端。
6. 大厅房间关闭时同步关闭 GameRoom；若游戏内还有玩家，通知并移出（连接保留）。
7. 删除准备/取消准备机制及其协议、模型、handler、状态。

## 2. 非目标

- 不新增网络层入口给外部系统；本次只提供服务端内部 API，预留将来的 HTTP/管理端调用。
- 不做登录鉴权、身份校验、协议版本协商。
- 不做 GameServer 断线后的房间对账/清理；`RoomCloseReason.GameServerOffline` 仅预留。
- 不扩展 `RoomListInfo` 字段。
- 不改变同步消息（Position/Animation/Spawn/Despawn）的广播逻辑。

## 3. 术语与数据归属

- **大厅房间（LobbyRoom）**：组队单元，拥有房主、人数上限、GameServer 地址。
- **游戏房间（GameRoom）**：GameServer 上的实际会话，只关心 `RoomId`、`RoomType`、`MaxPlayers` 和当前游戏内成员。
- **房主/房间类型/人数上限以 Lobby 为唯一事实来源**；GameServer 不保存房主，避免两条连接之间同步带来的竞态。

## 4. 数据模型

### 4.1 LobbyRoom

```csharp
public class LobbyRoom
{
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int MaxPlayers { get; set; }          // 0 = 无上限
    public List<long> PlayerIds { get; set; } = []; // 按加入顺序，决定房主继承顺序
    public long OwnerUserId { get; set; }        // 0 = 无主（系统房尚未有人加入）
    public long? CreatorUserId { get; set; }     // null = 外部/系统创建，仅用于日志
    public string GameServerAddress { get; set; } = string.Empty;
    public int GameServerPort { get; set; }
}
```

删除：`ReadyPlayerIds`、`IsStarted`、`GameCreated`。

### 4.2 GameRoom

```csharp
public class GameRoom
{
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int MaxPlayers { get; set; }
    public HashSet<long> PlayerIds { get; set; } = [];
}
```

删除：`OwnerUserId`、`AllowedPlayerIds`。

### 4.3 PlayerState

```csharp
public enum PlayerState { InLobby, InRoom }
```

删除：`Ready`、`InGame`（大厅不再感知玩家是否进入游戏服）。

## 5. 协议变更

### 5.1 删除的消息与模型

| 消息 ID | 名称 | 删除的模型 / handler |
|---|---|---|
| 20 | `GameReady` | `GameReadyRequest`、`GameReadyResponse`、`GameReadyHandler` |
| 21 | `GameUnready` | `GameUnreadyResponse`、`GameUnreadyHandler` |
| 22 | `GameStart` | `GameStartRequest`、`GameStartHandler` |
| 23 | `GameStartNotify` | `GameStartNotify` |
| 25 | `GameReadyNotify` | `GameReadyNotify` |
| 34 | `AuthorizeGamePlayer` | `AuthorizeGamePlayerRequest`、`AuthorizeGamePlayerHandler` |

`ReturnCode` 枚举保持现有数值不变；`NotRoomOwner`、`NotAllReady`、`NotAuthorized`、`GameServerOffline` 变为保留值，本次不删除。

### 5.2 调整的消息

| 消息 ID | 名称 | 变更 |
|---|---|---|
| 24 → 102 | `CreateGameRoom` | 改为服务端内部 ID（符合「内部 ≥100」约定）；在**大厅建房时**发送；字段改为 `RoomId`、`RoomType`、`MaxPlayers`（移除 `OwnerUserId`） |

### 5.3 新增的消息

客户端消息（<100）：

```csharp
MessageIds.RoomOwnerChangedNotify = 16; // { RoomId, OwnerUserId }
MessageIds.RoomClosedNotify       = 17; // { RoomId, RoomCloseReason }
```

服务端内部消息（>=100）：

```csharp
MessageIds.CloseGameRoom = 103; // { RoomId, RoomCloseReason }
```

模型：

```csharp
[MessagePackObject]
public class RoomOwnerChangedNotify
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public long OwnerUserId { get; set; }
}

[MessagePackObject]
public class RoomClosedNotify
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public RoomCloseReason Reason { get; set; }
}

[MessagePackObject]
public class CloseGameRoomRequest
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public RoomCloseReason Reason { get; set; }
}

public enum RoomCloseReason : byte
{
    Empty = 0,             // 最后一人离开
    ClosedBySystem = 1,    // 外部接口/管理端关闭
    GameServerOffline = 2  // 预留
}
```

### 5.4 最终 MessageIds 布局

```text
// 客户端
JoinLobby 1, LeaveLobby 2, Chat 3, ChatNotify 4
CreateRoom 10, JoinRoom 11, LeaveRoom 12, JoinRoomNotify 13, LeaveRoomNotify 14,
RoomList 15, RoomOwnerChangedNotify 16, RoomClosedNotify 17
JoinGame 30, LeaveGame 31, JoinGameNotify 32, LeaveGameNotify 33

// 服务端内部
GameServerRegister 100, GameServerUpdate 101, CreateGameRoom 102, CloseGameRoom 103
```

### 5.5 移除的响应字段

`JoinGameResponse` 移除 `OwnerUserId`，保留 `RoomId`、`RoomType`、`Players`。房主由 `CreateRoomResponse` / `JoinRoomResponse` 中的 `RoomInfo` 提供。

## 6. 大厅流程

### 6.1 CreateRoom（统一入口）

```csharp
public (CreateRoomResponse Response, ReturnCode Code)
    RoomManager.CreateRoom(CreateRoomRequest request, long? creatorUserId)
```

步骤：

1. 若 `creatorUserId` 有值，玩家必须在大厅中，否则 `NotInLobby`。
2. 校验 `RoomId` 不冲突，否则 `Error`。
3. 选择存活的 GameServer（`PickLeastLoaded`），没有则 `NoGameServerAvailable`。
4. 先向 GameServer 发送 `CreateGameRoom`。发送失败（peer 已断开）返回 `GameServerOffline`，此时旧房间和玩家状态都不改变。
5. 发送成功后：
   - 若 `creatorUserId` 有值：若玩家已在其他房间，先 `LeaveRoom(userId)`（旧房可能因此关闭并同步删除 GameRoom）；房间成员为 `[creatorUserId]`，`OwnerUserId = creatorUserId`；玩家状态置 `InRoom`。
   - 若 `creatorUserId` 为 null（系统房）：成员为空，`OwnerUserId = 0`，不修改任何玩家状态。
6. 写入房间表并返回 `RoomInfo`。

Handler 调用时传当前 peer 的 userId；外部调用走 `LobbyServer` 的内部 API：

```csharp
public (CreateRoomResponse Response, ReturnCode Code) CreateSystemRoom(CreateRoomRequest request);
public ReturnCode CloseRoom(string roomId);
```

### 6.2 JoinRoom

1. 房间不存在 → `RoomNotFound`。
2. `MaxPlayers > 0 && PlayerIds.Count >= MaxPlayers` → `RoomFull`（不再修改任何状态）。
3. 已在目标房 → `AlreadyInRoom`。
4. 在别的房 → 先 `LeaveRoom(userId)`。
5. 加入 `PlayerIds`（追加到末尾）。
6. 若 `OwnerUserId == 0` → 设为当前 userId（系统房首个加入者成为房主）。此时房间原本为空，无其他成员需要通知。
7. 玩家状态置 `InRoom`。
8. 向其他成员广播 `JoinRoomNotify`。
9. 返回 `RoomInfo`（含 GameServer 地址/端口、房主、成员列表）。

### 6.3 LeaveRoom

1. 玩家不在房间 → `NotInRoom`。
2. 从 `PlayerIds` 移除，`ReadyPlayerIds` 概念已删除；状态置 `InLobby`。
3. 若房间已空：
   - 从房间表删除；
   - 向对应 GameServer 发送 `CloseGameRoom(Empty)`；GameServer 不在线时只记录日志。
4. 若房间非空且离开者当前房主：
   - 新房主 = `PlayerIds` 的第一个元素（列表保持加入顺序，即「下一个加入者」）；
   - 向剩余成员广播 `RoomOwnerChangedNotify`。
5. 向剩余成员广播 `LeaveRoomNotify`。
6. 返回 `LeaveRoomResponse`。

### 6.4 CloseRoom（外部/管理端）

```csharp
public ReturnCode CloseRoom(string roomId, RoomCloseReason reason)
```

1. 房间不存在 → `RoomNotFound`。
2. 向所有大厅成员发送 `RoomClosedNotify(reason)`，状态置 `InLobby`。
3. 向 GameServer 发送 `CloseGameRoom(reason)`（GameServer 负责通知并移出游戏内玩家）；GameServer 不在线时只记录日志。
4. 删除大厅房间。

`LobbyServer.CloseRoom(string roomId)` 使用 `ClosedBySystem` 调用上述方法。

### 6.5 LeaveLobby / 断线

修正现有缺陷：不再直接 `_players.Remove`，改为调用 `RoomManager.RemovePlayer(peer)`——它内部先 `LeaveRoom`（必要时关闭并同步删除 GameRoom），再从 `PlayerManager` 移除该玩家。`LobbyManager.Leave` 与断线回调统一走这条路径。

### 6.6 RoomList

保持现有行为：返回所有房间的 `RoomId`、`RoomType`、`PlayerCount`。满员房间仍会出现，客户端加入时收到 `RoomFull`。

## 7. GameServer 流程

### 7.1 CreateGameRoom（幂等）

- 房间已存在：记录警告并保留现有 `PlayerIds`，不覆盖、不清空。
- 不存在：按 `RoomId`、`RoomType`、`MaxPlayers` 创建，`PlayerIds` 为空。

### 7.2 CloseGameRoom（强制关闭）

1. 房间不存在：记录日志后返回。
2. 快照当前 `PlayerIds`，对每个玩家：
   - 发送 `RoomClosedNotify(Reason)`；
   - 从 `GamePlayerManager` 移除（保留 NetPeer 连接）。
3. 从房间表删除。

### 7.3 JoinGame（移除授权）

1. 房间不存在 → `RoomNotFound`。
2. 当前 peer 已在任何游戏房 → `AlreadyInRoom`。
3. 房间中已存在该 userId → `AlreadyInRoom`。
4. 添加玩家并广播 `JoinGameNotify`，返回 `JoinGameResponse`。
5. `MaxPlayers` 仅由 Lobby 在 `JoinRoom` 时限制；GameServer 不重复校验。

不再检查 `AllowedPlayerIds`，也不再接收 `AuthorizeGamePlayer`。

### 7.4 LeaveGame / 断线

移除玩家并广播 `LeaveGameNotify`。房间变空时**不删除** `GameRoom`——生命周期由 Lobby 决定，只有 `CloseGameRoom` 才删除。这样玩家退出游戏后仍可重新 `JoinGame`，不会出现大厅房存在但游戏房已被删除的情况。

## 8. 客户端影响（TestClient）

- 删除 `ready`、`unready`、`start` 命令与对应消息处理。
- `CreateRoom` / `JoinRoom` 成功后：保存 `RoomId` 和 `RoomInfo.GameServerAddress/Port`，自动连接 GameServer；连接成功后发送 `JoinGame`。
- 离开或切换大厅房间时，客户端应先向 GameServer 发送 `LeaveGame`（成员各自管理，服务端不代发）。
- `JoinGame` 返回 `RoomNotFound` 时应在 1 秒内短暂重试：Lobby→GameServer 的 `CreateGameRoom` 与客户端 `JoinGame` 走不同连接，且没有 ACK。
- 处理 `RoomOwnerChangedNotify`（更新房主展示）和 `RoomClosedNotify`（清空本地房间状态，断开或保留 GameServer 连接由客户端决定）。
- 保留 `joingame <roomId>` 供手动重连。

## 9. 错误码与失败处理

| 场景 | 返回 |
|---|---|
| 无可用 GameServer（建房） | `NoGameServerAvailable` |
| CreateGameRoom 发送时 peer 已断开 | `GameServerOffline`，大厅不建房 |
| 房间不存在（加入/离开/关闭） | `RoomNotFound` / `NotInRoom` |
| 房间满 | `RoomFull` |
| 已在目标房 | `AlreadyInRoom` |
| 玩家不在大厅 | `NotInLobby` |
| 反序列化失败 | `DeserializeFailed` |

跨服同步没有 ACK：`CreateGameRoom` / `CloseGameRoom` 走 `ReliableOrdered`，依赖同一条 Lobby→GameServer 连接的顺序保证。客户端在 `JoinRoomResponse` 之后再连接 GameServer，正常时序足够；客户端和测试都应对 `JoinGame` 的 `RoomNotFound` 做短时间重试。`CreateGameRoom` 发送前会检查连接状态，但无法完全消除“选中 GameServer 后立即断线”的窗口；该情况下房间可能创建但不可用。

## 10. 测试计划

先写失败测试，再实现。集成测试使用真实 LobbyServer + GameServer。

1. `CreateRoom_PlayerCreator_IsOwnerAndMember`
2. `CreateRoom_SyncsGameRoom_JoinGameSucceedsWithoutStart`
3. `CreateSystemRoom_FirstJoiner_BecomesOwner`
4. `JoinRoom_WhenFull_ReturnsRoomFull_AndAllowsJoinAfterLeave`
5. `LeaveRoom_OwnerLeaves_TransfersToNextJoiner_AndNotifies`
6. `LeaveRoom_LastMember_RemovesGameRoom`
7. `CloseRoom_System_NotifiesLobbyAndKicksGamePlayers`
8. `JoinRoom_FromAnotherRoom_LeavesPreviousRoom`
9. `LeaveLobby_WhileInRoom_CleansUpMembership`
10. `CreateRoom_WhenNoGameServer_ReturnsNoGameServerAvailable`

需要删除/重写的旧测试：

- `JoinRoom_ToStartedRoom_ShouldFail_RoomFull` → 由 4 替代
- `QuickMatch_Room_Unrelated_Player_JoinGame_ShouldBeRejected` → 授权删除后改为「未知房间被拒绝」
- `StartGame_AfterGameServerGoesOffline_ShouldFail` → 由 10 替代

`ServerFixture` 需要暴露 `LobbyServer.CreateSystemRoom` / `CloseRoom`，并保留 `StopGameServer`。

## 11. 影响文件

**SharedLib**

- `Protocol/MessageIds.cs`
- `Models/CreateGameRoomRequest.cs`
- `Models/JoinGameResponse.cs`
- 删除 7 个准备/开局/授权模型，新增 `RoomOwnerChangedNotify`、`RoomClosedNotify`、`CloseGameRoomRequest`、`RoomCloseReason`

**LobbyServer**

- `Room/LobbyRoom.cs`、`Room/RoomManager.cs`
- `Lobby/LobbyManager.cs`（Leave 清理房间）
- `Handlers/CreateRoomHandler.cs`、`JoinRoomHandler.cs`、`LeaveRoomHandler.cs`
- 删除 `GameReadyHandler`、`GameUnreadyHandler`、`GameStartHandler`
- `LobbyServer.cs`（注册表、外部 API）
- `Player/PlayerState.cs`

**GameServer**

- `Room/GameRoom.cs`、`Room/GameRoomManager.cs`
- `Handlers/CreateGameRoomHandler.cs`
- 新增 `Handlers/CloseGameRoomHandler.cs`
- 删除 `Handlers/AuthorizeGamePlayerHandler.cs`
- `GameServer.cs`（注册表）

**其他**

- `TestClient/Program.cs`
- `GameServer.Tests/LobbyFlowTests.cs`、`ServerFixture.cs`
- `AGENTS.md`（协议、房间流程、房主规则、消息 ID 约定）

## 12. 发布与兼容

消息 ID 重排（`CreateGameRoom` 24 → 102）和多个模型字段变化要求 **LobbyServer 与 GameServer 锁步发布**。项目当前没有协议版本协商，部署时必须同时更新两个服务端与客户端。

## 13. 遗留风险

- 无授权后，知道 `RoomId` 的任何客户端都可以加入游戏房间；这是本次确认的行为。
- GameServer 断线后，大厅房间仍指向不可用的地址；后续需要单独的「断线房间清理/重连对账」设计。
- 外部创建的系统房如果从未被加入且未被 `CloseRoom`，GameRoom 会一直存在；由调用方负责关闭。
