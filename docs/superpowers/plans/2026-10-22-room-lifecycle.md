# 房间生命周期改造实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把大厅房间改造成组队房：建房即同步创建 GameRoom、房主归首个加入者、删除准备/开局/授权，并同步房间关闭。

**Architecture:** 保持现有分层：handler 只做转发，状态集中在 `RoomManager` / `GameRoomManager`。Lobby 是房间元数据（房主/类型/人数上限/GameServer 地址）的唯一事实来源，GameServer 只维护游戏内成员。跨服同步通过现有 `HandlerRegistry` + 内部消息完成，无 ACK，依赖 Lobby→GameServer 单连接上的 `ReliableOrdered` 顺序。

**Tech Stack:** .NET 10、LiteNetLib 2.1.4、MessagePack 3.1.7、Serilog、xunit 2.9.3

**Spec:** `docs/superpowers/specs/2026-10-22-room-lifecycle-design.md`

## Global Constraints

- 目标框架 `net10.0`；`dotnet build GameServer.sln` 必须 0 警告 0 错误。
- 不新增 NuGet 依赖。
- 服务端注释与日志使用中文，保持现有风格。
- 新增消息 ID：客户端消息 `<100`，服务端内部消息 `>=100`。
- handler 保持薄：只解析/转发，业务状态放在 manager。
- 测试是真实启动 LobbyServer + GameServer 的集成测试；不要 mock 服务器。
- LobbyServer 与 GameServer 必须锁步发布（消息 ID 重排，无协议版本协商）。
- 删除准备/开始/授权后，玩家加入大厅房间即可直接连 GameServer 发 `JoinGame`。

## Review Focus

- 玩家在 A 房时创建 B 房：A 房成员必须被清理，A 房未空时房主转移，A 房空时 GameRoom 必须被删除。测试：`CreateRoom_WhileInAnotherRoom_ClosesPreviousRoom`（Task 2）。
- 系统房从未有人加入时 `CloseRoom`：大厅没有通知对象，但 GameRoom 必须删除，之后 `JoinGame` 返回 `RoomNotFound`。测试：`CloseRoom_EmptySystemRoom_RemovesGameRoom`（Task 3）。
- 同一 peer 重复 `JoinLobby`：旧房间不得留下幽灵成员，房主按顺序转移。测试：`JoinLobby_Twice_RemovesPreviousRoomMembership`（Task 2）。
- 同一 peer 已在游戏房 A 再 `JoinGame` B：必须返回 `AlreadyInRoom`，不能同时挂在两个游戏房。测试：`JoinGame_WhenAlreadyInGameRoom_ReturnsAlreadyInRoom`（Task 3）。
- 重复 `CreateGameRoom`：不得替换 `GameRoom` 实例或清空成员。测试：`CreateRoom_Duplicate_KeepsSameRoomInstance`（Task 3）。

---

## Task 1: 新增房间生命周期协议模型

**Files:**
- Modify: `SharedLib/Protocol/MessageIds.cs`
- Modify: `SharedLib/Models/CreateGameRoomRequest.cs`
- Create: `SharedLib/Models/RoomCloseReason.cs`
- Create: `SharedLib/Models/RoomOwnerChangedNotify.cs`
- Create: `SharedLib/Models/RoomClosedNotify.cs`
- Create: `SharedLib/Models/CloseGameRoomRequest.cs`
- Test: `GameServer.Tests/ProtocolModelTests.cs`

**Interfaces:**
- Consumes: `SharedLib.Utils.MessageHelper.CreateFrame/Deserialize`
- Produces: `MessageIds.RoomOwnerChangedNotify = 16`、`MessageIds.RoomClosedNotify = 17`、`MessageIds.CloseGameRoom = 103`；`RoomCloseReason`、`RoomOwnerChangedNotify`、`RoomClosedNotify`、`CloseGameRoomRequest`；`CreateGameRoomRequest.MaxPlayers`

- [ ] **Step 1: 写失败的序列化测试**

Create `GameServer.Tests/ProtocolModelTests.cs`:

```csharp
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;

namespace GameServer.Tests;

public class ProtocolModelTests
{
    [Fact]
    public void NewRoomModels_RoundTrip()
    {
        var owner = RoundTrip(new RoomOwnerChangedNotify { RoomId = "r1", OwnerUserId = 42 });
        Assert.Equal("r1", owner.RoomId);
        Assert.Equal(42, owner.OwnerUserId);

        var closed = RoundTrip(new RoomClosedNotify { RoomId = "r1", Reason = RoomCloseReason.ClosedBySystem });
        Assert.Equal("r1", closed.RoomId);
        Assert.Equal(RoomCloseReason.ClosedBySystem, closed.Reason);

        var close = RoundTrip(new CloseGameRoomRequest { RoomId = "r1", Reason = RoomCloseReason.Empty });
        Assert.Equal(RoomCloseReason.Empty, close.Reason);

        var create = RoundTrip(new CreateGameRoomRequest
        {
            RoomId = "r1",
            RoomType = RoomType.QuickMatch,
            MaxPlayers = 8
        });
        Assert.Equal("r1", create.RoomId);
        Assert.Equal(RoomType.QuickMatch, create.RoomType);
        Assert.Equal(8, create.MaxPlayers);
    }

    private static T RoundTrip<T>(T value) where T : class
    {
        var frame = MessageHelper.CreateFrame(999, ReturnCode.Success, value);
        return MessageHelper.Deserialize<T>(frame)!;
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test GameServer.Tests --nologo --filter FullyQualifiedName~ProtocolModelTests`
Expected: 编译失败，提示 `RoomOwnerChangedNotify` / `RoomCloseReason` / `CloseGameRoomRequest` 不存在。

- [ ] **Step 3: 新增模型与消息 ID**

Create `SharedLib/Models/RoomCloseReason.cs`:

```csharp
namespace SharedLib.Models;

public enum RoomCloseReason : byte
{
    Empty = 0,
    ClosedBySystem = 1,
    GameServerOffline = 2
}
```

Create `SharedLib/Models/RoomOwnerChangedNotify.cs`:

```csharp
using MessagePack;

namespace SharedLib.Models;

[MessagePackObject]
public class RoomOwnerChangedNotify
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public long OwnerUserId { get; set; }
}
```

Create `SharedLib/Models/RoomClosedNotify.cs`:

```csharp
using MessagePack;

namespace SharedLib.Models;

[MessagePackObject]
public class RoomClosedNotify
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public RoomCloseReason Reason { get; set; }
}
```

Create `SharedLib/Models/CloseGameRoomRequest.cs`:

```csharp
using MessagePack;

namespace SharedLib.Models;

[MessagePackObject]
public class CloseGameRoomRequest
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public RoomCloseReason Reason { get; set; }
}
```

Modify `SharedLib/Models/CreateGameRoomRequest.cs`（保留 `OwnerUserId` 到 Task 4 再删，保证本任务编译通过）:

```csharp
using MessagePack;

namespace SharedLib.Models;

[MessagePackObject]
public class CreateGameRoomRequest
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public RoomType RoomType { get; set; }
    [Key(2)] public long OwnerUserId { get; set; }
    [Key(3)] public int MaxPlayers { get; set; }
}
```

Modify `SharedLib/Protocol/MessageIds.cs`，在客户端通知区与内部通信区分别加入：

```csharp
public const ushort RoomOwnerChangedNotify = 16;
public const ushort RoomClosedNotify = 17;
```

```csharp
public const ushort CloseGameRoom = 103;
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test GameServer.Tests --nologo --filter FullyQualifiedName~ProtocolModelTests`
Expected: PASS（1 个测试）。

- [ ] **Step 5: 编译并提交**

Run: `dotnet build GameServer.sln`
Expected: 0 警告 0 错误。

```bash
git add SharedLib/Protocol/MessageIds.cs SharedLib/Models/CreateGameRoomRequest.cs \
  SharedLib/Models/RoomCloseReason.cs SharedLib/Models/RoomOwnerChangedNotify.cs \
  SharedLib/Models/RoomClosedNotify.cs SharedLib/Models/CloseGameRoomRequest.cs \
  GameServer.Tests/ProtocolModelTests.cs
git commit -m "新增房间生命周期协议模型"
```

---

## Task 2: 大厅房间核心流程

**Files:**
- Modify: `LobbyServer/Room/LobbyRoom.cs`
- Modify: `LobbyServer/Room/RoomManager.cs`（整体替换）
- Modify: `LobbyServer/Lobby/LobbyManager.cs`
- Modify: `LobbyServer/Handlers/CreateRoomHandler.cs`
- Delete: `LobbyServer/Handlers/GameReadyHandler.cs`
- Delete: `LobbyServer/Handlers/GameUnreadyHandler.cs`
- Delete: `LobbyServer/Handlers/GameStartHandler.cs`
- Modify: `LobbyServer/LobbyServer.cs`
- Modify: `LobbyServer/Player/PlayerState.cs`
- Modify: `SharedLib/Handlers/HandlerRegistry.cs`
- Modify: `GameServer.Tests/ServerFixture.cs`
- Modify: `GameServer.Tests/TestPorts.cs`
- Modify: `GameServer.Tests/ServerFixture.cs` 中的 `TestClient.WaitMessage`（改为消费消息）
- Replace: `GameServer.Tests/LobbyFlowTests.cs`

**Interfaces:**
- Consumes: Task 1 的 `RoomOwnerChangedNotify`、`RoomClosedNotify`、`CloseGameRoomRequest`、`MessageIds.CloseGameRoom`
- Produces:
  - `RoomManager.CreateRoom(CreateRoomRequest request, long? creatorUserId) : (CreateRoomResponse, ReturnCode)`
  - `RoomManager.CloseRoom(string roomId, RoomCloseReason reason) : ReturnCode`
  - `LobbyServer.CreateSystemRoom(CreateRoomRequest request) : (CreateRoomResponse, ReturnCode)`
  - `LobbyServer.CloseRoom(string roomId) : ReturnCode`
  - `LobbyRoom.PlayerIds` 为按加入顺序的 `List<long>`
  - `TestClient.WaitMessage` 返回后移除该消息

- [ ] **Step 1: 写失败的大厅流程测试**

Replace `GameServer.Tests/LobbyFlowTests.cs`:

```csharp
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;

namespace GameServer.Tests;

public class LobbyFlowTests
{
    private readonly ServerFixture _fixture;

    public LobbyFlowTests()
    {
        _fixture = new ServerFixture();
    }

    private TestClient ConnectClient(long userId, string nickname)
    {
        var client = new TestClient();
        client.Connect(_fixture.LobbyClientPort, ServerFixture.ClientKey);
        Assert.True(client.WaitConnected(), "客户端未能在超时内连接");
        client.Send(MessageIds.JoinLobby, new JoinLobbyRequest
        {
            Player = new PlayerInfo { UserId = userId, Nickname = nickname }
        });
        var res = client.WaitPayload<JoinLobbyResponse>(MessageIds.JoinLobby);
        Assert.NotNull(res);
        return client;
    }

    private static CreateRoomResponse CreateRoom(TestClient client, string roomId,
        RoomType type = RoomType.Default, int maxPlayers = 0)
    {
        client.Send(MessageIds.CreateRoom, new CreateRoomRequest
        {
            RoomId = roomId,
            RoomType = type,
            MaxPlayers = maxPlayers
        });
        var res = client.WaitPayload<CreateRoomResponse>(MessageIds.CreateRoom);
        Assert.NotNull(res);
        return res!;
    }

    private static JoinRoomResponse JoinRoom(TestClient client, string roomId,
        ReturnCode expected = ReturnCode.Success)
    {
        client.Send(MessageIds.JoinRoom, new JoinRoomRequest { RoomId = roomId });
        var message = client.WaitMessage(MessageIds.JoinRoom);
        Assert.NotNull(message);
        Assert.Equal(expected, message!.Value.Code);
        return expected == ReturnCode.Success
            ? MessageHelper.Deserialize<JoinRoomResponse>(message.Value.Payload)!
            : new JoinRoomResponse();
    }

    [Fact]
    public void JoinLobby_ShouldSucceed()
    {
        using var client = ConnectClient(1001, "tester");
        Assert.True(client.Connected);
    }

    [Fact]
    public void CreateRoom_PlayerCreator_IsOwnerAndMember()
    {
        using var owner = ConnectClient(1101, "owner");

        var res = CreateRoom(owner, "room-owner");

        Assert.Equal("room-owner", res.Room.RoomId);
        Assert.Equal(1101, res.Room.OwnerUserId);
        Assert.Single(res.Room.Players);
        Assert.Equal(1101, res.Room.Players[0].UserId);
    }

    [Fact]
    public void CreateSystemRoom_FirstJoiner_BecomesOwner()
    {
        var (created, code) = _fixture.CreateSystemRoom("room-system");
        Assert.Equal(ReturnCode.Success, code);
        Assert.Equal(0, created.Room.OwnerUserId);
        Assert.Empty(created.Room.Players);

        using var joiner = ConnectClient(1201, "joiner");
        var joined = JoinRoom(joiner, "room-system");

        Assert.Equal(1201, joined.Room.OwnerUserId);
        Assert.Single(joined.Room.Players);
    }

    [Fact]
    public void JoinRoom_WhenFull_ReturnsRoomFull_AndAllowsJoinAfterLeave()
    {
        using var owner = ConnectClient(1301, "owner");
        using var second = ConnectClient(1302, "second");
        using var third = ConnectClient(1303, "third");

        CreateRoom(owner, "room-full", maxPlayers: 2);
        JoinRoom(second, "room-full");

        JoinRoom(third, "room-full", ReturnCode.RoomFull);

        second.Send(MessageIds.LeaveRoom, new LeaveRoomRequest());
        Assert.NotNull(second.WaitMessage(MessageIds.LeaveRoom));

        var joined = JoinRoom(third, "room-full");
        Assert.Equal(2, joined.Room.Players.Count);
    }

    [Fact]
    public void LeaveRoom_OwnerLeaves_TransfersToNextJoiner_AndNotifies()
    {
        using var owner = ConnectClient(1401, "owner");
        using var second = ConnectClient(1402, "second");
        using var third = ConnectClient(1403, "third");

        CreateRoom(owner, "room-transfer");
        JoinRoom(second, "room-transfer");
        JoinRoom(third, "room-transfer");

        owner.Send(MessageIds.LeaveRoom, new LeaveRoomRequest());
        Assert.NotNull(owner.WaitMessage(MessageIds.LeaveRoom));

        var secondNotify = second.WaitPayload<RoomOwnerChangedNotify>(MessageIds.RoomOwnerChangedNotify);
        var thirdNotify = third.WaitPayload<RoomOwnerChangedNotify>(MessageIds.RoomOwnerChangedNotify);

        Assert.NotNull(secondNotify);
        Assert.NotNull(thirdNotify);
        Assert.Equal(1402, secondNotify!.OwnerUserId);
        Assert.Equal(1402, thirdNotify!.OwnerUserId);
    }

    [Fact]
    public void JoinRoom_FromAnotherRoom_LeavesPreviousRoom()
    {
        using var first = ConnectClient(1501, "first");
        using var second = ConnectClient(1502, "second");
        using var keeper = ConnectClient(1503, "keeper");
        using var checker = ConnectClient(1504, "checker");

        CreateRoom(first, "room-a");
        JoinRoom(keeper, "room-a");
        CreateRoom(second, "room-b");

        JoinRoom(first, "room-b");

        var roomA = JoinRoom(checker, "room-a");
        Assert.DoesNotContain(roomA.Room.Players, p => p.UserId == 1501);
        Assert.Equal(1503, roomA.Room.OwnerUserId);
    }

    [Fact]
    public void CreateRoom_WhileInAnotherRoom_ClosesPreviousRoom()
    {
        using var first = ConnectClient(1601, "first");
        using var keeper = ConnectClient(1602, "keeper");
        using var checker = ConnectClient(1603, "checker");

        CreateRoom(first, "room-old");
        JoinRoom(keeper, "room-old");

        var newRoom = CreateRoom(first, "room-new");
        Assert.Equal("room-new", newRoom.Room.RoomId);

        var oldRoom = JoinRoom(checker, "room-old");
        Assert.DoesNotContain(oldRoom.Room.Players, p => p.UserId == 1601);
        Assert.Equal(1602, oldRoom.Room.OwnerUserId);
    }

    [Fact]
    public void LeaveLobby_WhileInRoom_CleansUpMembership()
    {
        using var owner = ConnectClient(1701, "owner");
        using var member = ConnectClient(1702, "member");
        using var checker = ConnectClient(1703, "checker");

        CreateRoom(owner, "room-leave-lobby");
        JoinRoom(member, "room-leave-lobby");

        owner.Send(MessageIds.LeaveLobby, new LeaveLobbyRequest { UserId = 1701 });
        Assert.NotNull(owner.WaitMessage(MessageIds.LeaveLobby));

        var joined = JoinRoom(checker, "room-leave-lobby");
        Assert.DoesNotContain(joined.Room.Players, p => p.UserId == 1701);
        Assert.Equal(1702, joined.Room.OwnerUserId);
    }

    [Fact]
    public void JoinLobby_Twice_RemovesPreviousRoomMembership()
    {
        using var owner = ConnectClient(1801, "owner");
        using var keeper = ConnectClient(1802, "keeper");
        using var checker = ConnectClient(1803, "checker");

        CreateRoom(owner, "room-relogin");
        JoinRoom(keeper, "room-relogin");

        owner.Send(MessageIds.JoinLobby, new JoinLobbyRequest
        {
            Player = new PlayerInfo { UserId = 1801, Nickname = "owner2" }
        });
        Assert.NotNull(owner.WaitMessage(MessageIds.JoinLobby));

        var joined = JoinRoom(checker, "room-relogin");
        Assert.DoesNotContain(joined.Room.Players, p => p.UserId == 1801);
        Assert.Equal(1802, joined.Room.OwnerUserId);
    }

    [Fact]
    public void CreateRoom_DuplicateRoomId_ReturnsError()
    {
        using var owner = ConnectClient(1901, "owner");
        CreateRoom(owner, "room-dup");

        owner.Send(MessageIds.CreateRoom, new CreateRoomRequest { RoomId = "room-dup" });
        var message = owner.WaitMessage(MessageIds.CreateRoom);

        Assert.NotNull(message);
        Assert.Equal(ReturnCode.Error, message!.Value.Code);
    }

    [Fact]
    public void CreateRoom_WhenNoGameServer_ReturnsNoGameServerAvailable()
    {
        using var owner = ConnectClient(2001, "owner");
        _fixture.StopGameServer();

        owner.Send(MessageIds.CreateRoom, new CreateRoomRequest { RoomId = "room-no-gs" });
        var message = owner.WaitMessage(MessageIds.CreateRoom);

        Assert.NotNull(message);
        Assert.Equal(ReturnCode.NoGameServerAvailable, message!.Value.Code);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test GameServer.Tests --nologo --filter FullyQualifiedName~LobbyFlowTests`
Expected: 编译失败，提示 `CreateSystemRoom` 不存在（以及 `CreateRoom` 旧实现没有 `creatorUserId` 参数）。

- [ ] **Step 3: 改造 LobbyRoom**

Replace `LobbyServer/Room/LobbyRoom.cs`:

```csharp
using SharedLib.Models;

namespace LobbyServer.Room;

public class LobbyRoom
{
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int MaxPlayers { get; set; }
    public List<long> PlayerIds { get; set; } = [];
    public long OwnerUserId { get; set; }
    public long? CreatorUserId { get; set; }
    public string GameServerAddress { get; set; } = string.Empty;
    public int GameServerPort { get; set; }
}
```

- [ ] **Step 4: 重写 RoomManager**

Replace `LobbyServer/Room/RoomManager.cs`:

```csharp
using LiteNetLib;
using LobbyServer.Cluster;
using LobbyServer.Player;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;

namespace LobbyServer.Room;

public class RoomManager
{
    private readonly PlayerManager _players;
    private readonly GameServerRegistry _gameServerRegistry;
    private readonly Dictionary<string, LobbyRoom> _rooms = new();

    public RoomManager(PlayerManager players, GameServerRegistry gameServerRegistry)
    {
        _players = players;
        _gameServerRegistry = gameServerRegistry;
    }

    public (CreateRoomResponse Response, ReturnCode Code) CreateRoom(CreateRoomRequest request, long? creatorUserId)
    {
        Log.Information("[RoomManager] 创建房间 creator={Creator} roomId={RoomId}", creatorUserId, request.RoomId);

        if (creatorUserId.HasValue && _players.Get(creatorUserId.Value) == null)
        {
            Log.Warning("[RoomManager] 创建房间失败：玩家不在大厅 userId={UserId}", creatorUserId);
            return (new CreateRoomResponse(), ReturnCode.NotInLobby);
        }

        var roomId = string.IsNullOrWhiteSpace(request.RoomId)
            ? Guid.NewGuid().ToString("N")[..8]
            : request.RoomId;

        if (_rooms.ContainsKey(roomId))
        {
            Log.Warning("[RoomManager] 创建房间失败：房间已存在 roomId={RoomId}", roomId);
            return (new CreateRoomResponse(), ReturnCode.Error);
        }

        var gs = PickGameServer();
        if (gs == null)
        {
            Log.Warning("[RoomManager] 创建房间失败：无可用GameServer");
            return (new CreateRoomResponse(), ReturnCode.NoGameServerAvailable);
        }

        var gsValue = gs.Value;

        try
        {
            MessageHelper.Send(gsValue.Key, MessageIds.CreateGameRoom, ReturnCode.Success, new CreateGameRoomRequest
            {
                RoomId = roomId,
                RoomType = request.RoomType,
                MaxPlayers = request.MaxPlayers,
                OwnerUserId = creatorUserId ?? 0
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[RoomManager] 同步创建游戏房间失败 roomId={RoomId}", roomId);
            return (new CreateRoomResponse(), ReturnCode.GameServerOffline);
        }

        if (creatorUserId.HasValue && _players.Get(creatorUserId.Value)?.CurrentRoomId != null)
            LeaveRoom(creatorUserId.Value);

        var room = new LobbyRoom
        {
            RoomId = roomId,
            RoomType = request.RoomType,
            MaxPlayers = request.MaxPlayers,
            OwnerUserId = creatorUserId ?? 0,
            CreatorUserId = creatorUserId,
            GameServerAddress = gsValue.Value.Address,
            GameServerPort = gsValue.Value.Port
        };

        if (creatorUserId.HasValue)
        {
            room.PlayerIds.Add(creatorUserId.Value);
            _players.SetState(creatorUserId.Value, PlayerState.InRoom, roomId);
        }

        _rooms[roomId] = room;

        Log.Information(
            "[RoomManager] 房间创建成功 roomId={RoomId} type={RoomType} owner={Owner} GameServer={Addr}:{Port} 成员数={Count}",
            roomId, room.RoomType, room.OwnerUserId, room.GameServerAddress, room.GameServerPort,
            room.PlayerIds.Count);

        return (new CreateRoomResponse { Room = BuildRoomInfo(room) }, ReturnCode.Success);
    }

    public (JoinRoomResponse Response, ReturnCode Code) JoinRoom(long userId, JoinRoomRequest request)
    {
        Log.Information("[RoomManager] 加入房间 userId={UserId} roomId={RoomId}", userId, request.RoomId);

        var player = _players.Get(userId);
        if (player == null)
        {
            Log.Warning("[RoomManager] 加入房间失败：玩家不存在 userId={UserId}", userId);
            return (new JoinRoomResponse { Room = new RoomInfo { RoomId = request.RoomId } }, ReturnCode.NotInLobby);
        }

        if (!_rooms.TryGetValue(request.RoomId, out var room))
        {
            Log.Warning("[RoomManager] 加入房间失败：房间未找到 roomId={RoomId}", request.RoomId);
            return (new JoinRoomResponse { Room = new RoomInfo { RoomId = request.RoomId } }, ReturnCode.RoomNotFound);
        }

        if (room.MaxPlayers > 0 && room.PlayerIds.Count >= room.MaxPlayers)
        {
            Log.Warning("[RoomManager] 加入房间失败：房间已满 roomId={RoomId} max={Max}",
                request.RoomId, room.MaxPlayers);
            return (new JoinRoomResponse { Room = new RoomInfo { RoomId = request.RoomId } }, ReturnCode.RoomFull);
        }

        if (room.PlayerIds.Contains(userId))
        {
            Log.Warning("[RoomManager] 加入房间失败：已在房间中 roomId={RoomId} userId={UserId}",
                request.RoomId, userId);
            return (new JoinRoomResponse { Room = BuildRoomInfo(room) }, ReturnCode.AlreadyInRoom);
        }

        if (player.CurrentRoomId != null)
            LeaveRoom(userId);

        room.PlayerIds.Add(userId);
        if (room.OwnerUserId == 0)
            room.OwnerUserId = userId;

        _players.SetState(userId, PlayerState.InRoom, request.RoomId);

        var notify = new JoinRoomNotify { RoomId = room.RoomId, Player = player.Info };
        foreach (var otherId in room.PlayerIds.Where(id => id != userId))
        {
            var otherPlayer = _players.Get(otherId);
            if (otherPlayer != null)
                MessageHelper.Send(otherPlayer.Peer, MessageIds.JoinRoomNotify, ReturnCode.Success, notify);
        }

        Log.Information("[RoomManager] 加入房间成功 roomId={RoomId} userId={UserId} 房主={Owner} 房间人数={Count}",
            room.RoomId, userId, room.OwnerUserId, room.PlayerIds.Count);

        return (new JoinRoomResponse { Room = BuildRoomInfo(room) }, ReturnCode.Success);
    }

    public (LeaveRoomResponse Response, ReturnCode Code) LeaveRoom(long userId)
    {
        Log.Information("[RoomManager] 离开房间 userId={UserId}", userId);

        var player = _players.Get(userId);
        if (player == null || player.CurrentRoomId == null)
            return (new LeaveRoomResponse(), ReturnCode.NotInRoom);

        var roomId = player.CurrentRoomId;
        if (!_rooms.TryGetValue(roomId, out var room))
            return (new LeaveRoomResponse(), ReturnCode.NotInRoom);

        if (!room.PlayerIds.Remove(userId))
            return (new LeaveRoomResponse(), ReturnCode.NotInRoom);

        _players.SetState(userId, PlayerState.InLobby);

        var remaining = room.PlayerIds.Count;
        Log.Information("[RoomManager] 离开房间 roomId={RoomId} userId={UserId} 剩余人数={Count}",
            roomId, userId, remaining);

        if (remaining == 0)
        {
            _rooms.Remove(roomId);
            SendCloseGameRoom(room, RoomCloseReason.Empty);
            Log.Information("[RoomManager] 房间关闭 roomId={RoomId}（无玩家）", roomId);
        }
        else
        {
            if (room.OwnerUserId == userId)
            {
                room.OwnerUserId = room.PlayerIds[0];
                Broadcast(room, MessageIds.RoomOwnerChangedNotify, new RoomOwnerChangedNotify
                {
                    RoomId = roomId,
                    OwnerUserId = room.OwnerUserId
                });
                Log.Information("[RoomManager] 房主转移 roomId={RoomId} →{NewOwner}", roomId, room.OwnerUserId);
            }

            Broadcast(room, MessageIds.LeaveRoomNotify, new LeaveRoomNotify
            {
                RoomId = roomId,
                UserId = userId
            });
        }

        return (new LeaveRoomResponse { RoomId = roomId }, ReturnCode.Success);
    }

    public ReturnCode CloseRoom(string roomId, RoomCloseReason reason)
    {
        if (!_rooms.Remove(roomId, out var room))
        {
            Log.Warning("[RoomManager] 关闭房间失败：房间未找到 roomId={RoomId}", roomId);
            return ReturnCode.RoomNotFound;
        }

        var frame = MessageHelper.CreateFrame(MessageIds.RoomClosedNotify, ReturnCode.Success,
            new RoomClosedNotify { RoomId = roomId, Reason = reason });

        var peers = new List<NetPeer>();
        foreach (var userId in room.PlayerIds)
        {
            var player = _players.Get(userId);
            if (player == null)
                continue;

            peers.Add(player.Peer);
            _players.SetState(userId, PlayerState.InLobby);
        }

        MessageHelper.SendToAll(peers, frame, DeliveryMethod.ReliableOrdered);
        SendCloseGameRoom(room, reason);

        Log.Information("[RoomManager] 房间关闭 roomId={RoomId} reason={Reason} 成员数={Count}",
            roomId, reason, peers.Count);
        return ReturnCode.Success;
    }

    public RoomListResponse GetRoomList()
    {
        var list = _rooms.Values.Select(r => new RoomListInfo
        {
            RoomId = r.RoomId,
            RoomType = r.RoomType,
            PlayerCount = r.PlayerIds.Count
        }).ToList();

        Log.Information("[RoomManager] 查询房间列表 房间数={Count}", list.Count);
        return new RoomListResponse { Rooms = list };
    }

    public void RemovePlayer(NetPeer peer)
    {
        var player = _players.GetByPeer(peer);
        if (player == null)
            return;

        var userId = player.Info.UserId;
        Log.Information("[RoomManager] 移除玩家 userId={UserId}", userId);
        LeaveRoom(userId);
        _players.Remove(userId);
    }

    private void Broadcast(LobbyRoom room, ushort messageId, object data)
    {
        var frame = MessageHelper.CreateFrame(messageId, ReturnCode.Success, data);
        var peers = room.PlayerIds
            .Select(id => _players.Get(id))
            .Where(p => p != null)
            .Select(p => p!.Peer);

        MessageHelper.SendToAll(peers, frame, DeliveryMethod.ReliableOrdered);
    }

    private void SendCloseGameRoom(LobbyRoom room, RoomCloseReason reason)
    {
        var gs = _gameServerRegistry.GetByAddressPort(room.GameServerAddress, room.GameServerPort);
        if (gs == null)
        {
            Log.Warning("[RoomManager] GameServer离线，跳过关闭游戏房间 roomId={RoomId}", room.RoomId);
            return;
        }

        try
        {
            MessageHelper.Send(gs.Peer, MessageIds.CloseGameRoom, ReturnCode.Success, new CloseGameRoomRequest
            {
                RoomId = room.RoomId,
                Reason = reason
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[RoomManager] 关闭游戏房间失败 roomId={RoomId}", room.RoomId);
        }
    }

    private RoomInfo BuildRoomInfo(LobbyRoom room)
    {
        var gs = _gameServerRegistry.GetByAddressPort(room.GameServerAddress, room.GameServerPort);

        return new RoomInfo
        {
            RoomId = room.RoomId,
            RoomType = room.RoomType,
            GameServerAddress = gs?.Info.Address ?? room.GameServerAddress,
            GameServerPort = gs?.Info.Port ?? room.GameServerPort,
            OwnerUserId = room.OwnerUserId,
            Players = room.PlayerIds
                .Select(id => _players.Get(id))
                .Where(p => p != null)
                .Select(p => p!.Info)
                .ToList()
        };
    }

    private KeyValuePair<NetPeer, GameServerInfo>? PickGameServer()
    {
        var result = _gameServerRegistry.PickLeastLoaded();
        if (result is { } gs)
        {
            Log.Information("[RoomManager] 选择 GameServer {Addr}:{Port} 负载={Players}",
                gs.Value.Address, gs.Value.Port, gs.Value.PlayerCount);
        }
        else
        {
            Log.Warning("[RoomManager] 选择GameServer失败：无可用GameServer");
        }

        return result;
    }
}
```

- [ ] **Step 5: 更新大厅 handler、注册与外部 API**

Modify `LobbyServer/Handlers/CreateRoomHandler.cs` 的 `HandleMessage`:

```csharp
public override void HandleMessage(NetPeer peer, CreateRoomRequest request)
{
    var userId = Players.GetUserId(peer);
    var (res, code) = _rooms.CreateRoom(request, userId);
    MessageHelper.Send(peer, MessageId, code, res);
}
```

Delete:

```bash
rm LobbyServer/Handlers/GameReadyHandler.cs
rm LobbyServer/Handlers/GameUnreadyHandler.cs
rm LobbyServer/Handlers/GameStartHandler.cs
```

Modify `LobbyServer/LobbyServer.cs` 的 `RegisterHandlers()`，客户端 registry 只保留：

```csharp
_clientRegistry.Register(
    new JoinLobbyHandler(_lobbyManager),
    new LeaveLobbyHandler(_lobbyManager),
    new ChatHandler(_lobbyManager),
    new CreateRoomHandler(_players, _roomManager),
    new JoinRoomHandler(_players, _roomManager),
    new LeaveRoomHandler(_players, _roomManager),
    new RoomListHandler(_roomManager)
);
```

在 `PollEvents()` 之前加入内部 API：

```csharp
/// <summary>服务端内部 API：由外部系统创建房间，首个加入者成为房主</summary>
public (CreateRoomResponse Response, ReturnCode Code) CreateSystemRoom(CreateRoomRequest request)
{
    return _roomManager.CreateRoom(request, null);
}

/// <summary>服务端内部 API：关闭房间并同步关闭游戏房间</summary>
public ReturnCode CloseRoom(string roomId)
{
    return _roomManager.CloseRoom(roomId, RoomCloseReason.ClosedBySystem);
}
```

- [ ] **Step 6: 修正 LobbyManager 的重复登录与离开大厅**

Modify `LobbyServer/Lobby/LobbyManager.cs` 的 `Join`：

```csharp
var existing = _players.Get(userId);
if (existing != null)
{
    Log.Warning("[LobbyManager] 替换已有连接 userId={UserId}", userId);
    _rooms.RemovePlayer(existing.Peer);
    if (existing.Peer != peer)
        _netManager.DisconnectPeer(existing.Peer);
}
```

Modify `Leave`，把 `_players.Remove(request.UserId);` 替换为：

```csharp
_rooms.RemovePlayer(peer);
```

- [ ] **Step 7: 删除 PlayerState 的多余状态**

Replace `LobbyServer/Player/PlayerState.cs`:

```csharp
namespace LobbyServer.Player;

public enum PlayerState
{
    InLobby,
    InRoom
}
```

- [ ] **Step 8: 未知消息不再回显，避免跨服错误循环**

本任务开始发送 `CloseGameRoom`，但 GameServer 的 handler 要到 Task 3 才注册。现有 `HandlerRegistry` 对未知消息会回发同 ID 的 `Error`，两个服务端会互相回显形成死循环。

Replace `SharedLib/Handlers/HandlerRegistry.cs` 的 `Handle`：

```csharp
public bool Handle(NetPeer peer, ushort messageId, byte[] payload)
{
    if (!_handlers.TryGetValue(messageId, out var handler))
    {
        Log.Warning("未知消息ID messageId={MessageId}", messageId);
        return false;
    }

    handler.Handle(peer, messageId, payload);
    return true;
}
```

- [ ] **Step 9: 调整测试基建**

先让端口分配一次拿到三个互不相同的端口，避免并行测试复用到同一个端口。

Replace `GameServer.Tests/TestPorts.cs`:

```csharp
using System.Net;
using System.Net.Sockets;

namespace GameServer.Tests;

public static class TestPorts
{
    public static int[] NextFree(int count)
    {
        var listeners = new List<TcpListener>();
        var ports = new List<int>();

        try
        {
            for (var i = 0; i < count; i++)
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                listeners.Add(listener);
                ports.Add(((IPEndPoint)listener.LocalEndpoint).Port);
            }
        }
        finally
        {
            foreach (var listener in listeners)
                listener.Stop();
        }

        return ports.ToArray();
    }
}
```

Modify `GameServer.Tests/ServerFixture.cs` 的构造函数：

```csharp
var ports = TestPorts.NextFree(3);
LobbyClientPort = ports[0];
LobbyServerPort = ports[1];
GameServerPort = ports[2];
```

在 `ServerFixture` 类中加入：

```csharp
public (CreateRoomResponse Response, ReturnCode Code) CreateSystemRoom(string roomId,
    RoomType roomType = RoomType.Default, int maxPlayers = 0)
{
    return _lobby.CreateSystemRoom(new CreateRoomRequest
    {
        RoomId = roomId,
        RoomType = roomType,
        MaxPlayers = maxPlayers
    });
}

public ReturnCode CloseRoom(string roomId)
{
    return _lobby.CloseRoom(roomId);
}
```

把 `TestClient.WaitMessage` 改为消费消息（避免旧响应干扰后续断言）：

```csharp
public (ushort MessageId, ReturnCode Code, byte[] Payload)? WaitMessage(ushort messageId, int timeoutMs = 5000)
{
    var deadline = Environment.TickCount64 + timeoutMs;
    while (Environment.TickCount64 < deadline)
    {
        Poll();
        lock (_lock)
        {
            var index = _messages.FindIndex(m => m.MessageId == messageId);
            if (index >= 0)
            {
                var message = _messages[index];
                _messages.RemoveAt(index);
                return message;
            }
        }

        Thread.Sleep(5);
    }

    return null;
}
```

- [ ] **Step 10: 运行大厅测试确认通过**

Run: `dotnet test GameServer.Tests --nologo --filter FullyQualifiedName~LobbyFlowTests`
Expected: 全部 PASS。

- [ ] **Step 11: 编译并提交**

Run: `dotnet build GameServer.sln`
Expected: 0 警告 0 错误。

```bash
git add -A LobbyServer GameServer.Tests
git commit -m "重构大厅房间生命周期"
```

---

## Task 3: GameServer 房间生命周期

**Files:**
- Modify: `GameServer/Room/GameRoom.cs`
- Modify: `GameServer/Room/GameRoomManager.cs`
- Create: `GameServer/Handlers/CloseGameRoomHandler.cs`
- Delete: `GameServer/Handlers/AuthorizeGamePlayerHandler.cs`
- Modify: `GameServer/GameServer.cs`
- Test: `GameServer.Tests/GameRoomLifecycleTests.cs`
- Test: `GameServer.Tests/GameRoomManagerTests.cs`

**Interfaces:**
- Consumes: Task 1 的 `CloseGameRoomRequest`、`RoomClosedNotify`；Task 2 的 `LobbyServer.CreateSystemRoom/CloseRoom`、`ServerFixture.CreateSystemRoom/CloseRoom`
- Produces:
  - `GameRoomManager.CloseRoom(CloseGameRoomRequest request) : void`
  - `GameRoomManager.GetRoom(string roomId) : GameRoom?`
  - `GameRoomManager.CreateRoom(CreateGameRoomRequest request)` 幂等
  - `GameRoomManager.JoinGame(NetPeer peer, JoinGameRequest request)` 不再校验授权

- [ ] **Step 1: 写失败的 GameServer 生命周期测试**

Create `GameServer.Tests/GameRoomLifecycleTests.cs`:

```csharp
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;

namespace GameServer.Tests;

public class GameRoomLifecycleTests
{
    private readonly ServerFixture _fixture;

    public GameRoomLifecycleTests()
    {
        _fixture = new ServerFixture();
    }

    private TestClient ConnectLobbyClient(long userId, string nickname)
    {
        var client = new TestClient();
        client.Connect(_fixture.LobbyClientPort, ServerFixture.ClientKey);
        Assert.True(client.WaitConnected(), "客户端未能在超时内连接大厅");
        client.Send(MessageIds.JoinLobby, new JoinLobbyRequest
        {
            Player = new PlayerInfo { UserId = userId, Nickname = nickname }
        });
        Assert.NotNull(client.WaitPayload<JoinLobbyResponse>(MessageIds.JoinLobby));
        return client;
    }

    private TestClient ConnectGameClient()
    {
        var client = new TestClient();
        client.Connect(_fixture.GameServerPort, ServerFixture.ClientKey);
        Assert.True(client.WaitConnected(), "客户端未能在超时内连接游戏服");
        return client;
    }

    private static CreateRoomResponse CreateRoom(TestClient client, string roomId, int maxPlayers = 0)
    {
        client.Send(MessageIds.CreateRoom, new CreateRoomRequest
        {
            RoomId = roomId,
            RoomType = RoomType.Default,
            MaxPlayers = maxPlayers
        });
        var res = client.WaitPayload<CreateRoomResponse>(MessageIds.CreateRoom);
        Assert.NotNull(res);
        return res!;
    }

    private static JoinRoomResponse JoinRoom(TestClient client, string roomId)
    {
        client.Send(MessageIds.JoinRoom, new JoinRoomRequest { RoomId = roomId });
        var res = client.WaitPayload<JoinRoomResponse>(MessageIds.JoinRoom);
        Assert.NotNull(res);
        return res!;
    }

    private static (ushort MessageId, ReturnCode Code, byte[] Payload) JoinGameUntil(
        TestClient client, string roomId, long userId, Func<ReturnCode, bool> condition, int timeoutMs = 3000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        var last = ReturnCode.Error;

        while (Environment.TickCount64 < deadline)
        {
            client.Send(MessageIds.JoinGame, new JoinGameRequest
            {
                RoomId = roomId,
                Player = new PlayerInfo { UserId = userId, Nickname = "p" + userId }
            });

            var message = client.WaitMessage(MessageIds.JoinGame, 300);
            if (message is { } m)
            {
                last = m.Code;
                if (condition(m.Code))
                    return m;
            }
        }

        throw new InvalidOperationException($"JoinGame 未在超时内满足条件，最后一次返回 {last}");
    }

    [Fact]
    public void CreateRoom_SyncsGameRoom_JoinGameSucceeds()
    {
        using var lobby = ConnectLobbyClient(3101, "owner");
        CreateRoom(lobby, "game-sync");

        using var game = ConnectGameClient();
        var message = JoinGameUntil(game, "game-sync", 3101, code => code == ReturnCode.Success);

        Assert.Equal(ReturnCode.Success, message.Code);
        var response = MessageHelper.Deserialize<JoinGameResponse>(message.Payload);
        Assert.NotNull(response);
        Assert.Equal("game-sync", response!.RoomId);
    }

    [Fact]
    public void JoinGame_UnknownRoom_ReturnsRoomNotFound()
    {
        using var game = ConnectGameClient();
        var message = JoinGameUntil(game, "no-such-room", 3201, code => code == ReturnCode.RoomNotFound);

        Assert.Equal(ReturnCode.RoomNotFound, message.Code);
    }

    [Fact]
    public void JoinGame_WhenAlreadyInGameRoom_ReturnsAlreadyInRoom()
    {
        using var lobbyA = ConnectLobbyClient(3301, "a");
        using var lobbyB = ConnectLobbyClient(3302, "b");
        CreateRoom(lobbyA, "game-a");
        CreateRoom(lobbyB, "game-b");

        using var game = ConnectGameClient();
        JoinGameUntil(game, "game-a", 3301, code => code == ReturnCode.Success);

        game.Send(MessageIds.JoinGame, new JoinGameRequest
        {
            RoomId = "game-b",
            Player = new PlayerInfo { UserId = 3301, Nickname = "a" }
        });

        var message = game.WaitMessage(MessageIds.JoinGame);
        Assert.NotNull(message);
        Assert.Equal(ReturnCode.AlreadyInRoom, message!.Value.Code);
    }

    [Fact]
    public void CloseRoom_System_NotifiesLobbyAndKicksGamePlayers()
    {
        var (_, code) = _fixture.CreateSystemRoom("game-close");
        Assert.Equal(ReturnCode.Success, code);

        using var lobby = ConnectLobbyClient(3401, "p1");
        JoinRoom(lobby, "game-close");

        using var game = ConnectGameClient();
        JoinGameUntil(game, "game-close", 3401, c => c == ReturnCode.Success);

        Assert.Equal(ReturnCode.Success, _fixture.CloseRoom("game-close"));

        var lobbyClosed = lobby.WaitPayload<RoomClosedNotify>(MessageIds.RoomClosedNotify);
        var gameClosed = game.WaitPayload<RoomClosedNotify>(MessageIds.RoomClosedNotify);

        Assert.NotNull(lobbyClosed);
        Assert.NotNull(gameClosed);
        Assert.Equal(RoomCloseReason.ClosedBySystem, lobbyClosed!.Reason);
        Assert.Equal(RoomCloseReason.ClosedBySystem, gameClosed!.Reason);
    }

    [Fact]
    public void CloseRoom_EmptySystemRoom_RemovesGameRoom()
    {
        var (_, code) = _fixture.CreateSystemRoom("game-empty");
        Assert.Equal(ReturnCode.Success, code);

        Assert.Equal(ReturnCode.Success, _fixture.CloseRoom("game-empty"));

        using var game = ConnectGameClient();
        var message = JoinGameUntil(game, "game-empty", 3501, c => c == ReturnCode.RoomNotFound);
        Assert.Equal(ReturnCode.RoomNotFound, message.Code);
    }

    [Fact]
    public void LeaveRoom_LastMember_RemovesGameRoom()
    {
        using var lobby = ConnectLobbyClient(3601, "owner");
        CreateRoom(lobby, "game-leave");

        using var game = ConnectGameClient();
        JoinGameUntil(game, "game-leave", 3601, c => c == ReturnCode.Success);

        lobby.Send(MessageIds.LeaveRoom, new LeaveRoomRequest());
        Assert.NotNull(lobby.WaitMessage(MessageIds.LeaveRoom));

        var message = JoinGameUntil(game, "game-leave", 3601, c => c == ReturnCode.RoomNotFound);
        Assert.Equal(ReturnCode.RoomNotFound, message.Code);
    }
}
```

Create `GameServer.Tests/GameRoomManagerTests.cs`:

```csharp
using GameServer.Player;
using GameServer.Room;
using SharedLib.Models;

namespace GameServer.Tests;

public class GameRoomManagerTests
{
    [Fact]
    public void CreateRoom_Duplicate_KeepsSameRoomInstance()
    {
        var manager = new GameRoomManager(new GamePlayerManager());
        var request = new CreateGameRoomRequest
        {
            RoomId = "dup",
            RoomType = RoomType.Default,
            MaxPlayers = 4
        };

        manager.CreateRoom(request);
        var first = manager.GetRoom("dup");
        manager.CreateRoom(request);
        var second = manager.GetRoom("dup");

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Equal(1, manager.RoomCount);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test GameServer.Tests --nologo --filter FullyQualifiedName~GameRoom`
Expected: 编译失败，提示 `GameRoomManager.CloseRoom` / `GetRoom` 不存在。

- [ ] **Step 3: 改造 GameRoom**

Replace `GameServer/Room/GameRoom.cs`（本任务保留 `OwnerUserId` 到 Task 4）：

```csharp
using SharedLib.Models;

namespace GameServer.Room;

public class GameRoom
{
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public long OwnerUserId { get; set; }
    public int MaxPlayers { get; set; }
    public HashSet<long> PlayerIds { get; set; } = [];
}
```

- [ ] **Step 4: 改造 GameRoomManager**

Replace `GameServer/Room/GameRoomManager.cs`:

```csharp
using System.Collections.Concurrent;
using LiteNetLib;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using GameServer.Player;

namespace GameServer.Room;

public class GameRoomManager
{
    private readonly GamePlayerManager _playerManager;
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    public int PlayerCount => _playerManager.Count;

    public int RoomCount => _rooms.Count;

    public GameRoomManager(GamePlayerManager playerManager)
    {
        _playerManager = playerManager;
    }

    public GameRoom? GetRoom(string roomId)
    {
        return _rooms.TryGetValue(roomId, out var room) ? room : null;
    }

    public void CreateRoom(CreateGameRoomRequest request)
    {
        var room = new GameRoom
        {
            RoomId = request.RoomId,
            RoomType = request.RoomType,
            OwnerUserId = request.OwnerUserId,
            MaxPlayers = request.MaxPlayers
        };

        if (!_rooms.TryAdd(request.RoomId, room))
        {
            Log.Warning("[GameRoomManager] 游戏房间已存在，忽略重复创建 roomId={RoomId}", request.RoomId);
            return;
        }

        Log.Information("[GameRoomManager] 游戏房间创建 roomId={RoomId} type={RoomType} maxPlayers={Max}",
            request.RoomId, request.RoomType, request.MaxPlayers);
    }

    public void CloseRoom(CloseGameRoomRequest request)
    {
        if (!_rooms.TryRemove(request.RoomId, out var room))
        {
            Log.Warning("[GameRoomManager] 关闭游戏房间失败：房间未找到 roomId={RoomId}", request.RoomId);
            return;
        }

        var notify = new RoomClosedNotify { RoomId = request.RoomId, Reason = request.Reason };
        foreach (var userId in room.PlayerIds)
        {
            var player = _playerManager.Get(userId);
            if (player == null)
                continue;

            MessageHelper.Send(player.Peer, MessageIds.RoomClosedNotify, ReturnCode.Success, notify);
            _playerManager.Remove(userId);
        }

        Log.Information("[GameRoomManager] 游戏房间关闭 roomId={RoomId} reason={Reason} 移出玩家={Count}",
            request.RoomId, request.Reason, room.PlayerIds.Count);
    }

    public (JoinGameResponse Response, ReturnCode Code) JoinGame(NetPeer peer, JoinGameRequest request)
    {
        if (!_rooms.TryGetValue(request.RoomId, out var room))
        {
            Log.Warning("[GameRoomManager] 加入游戏失败：房间未找到 roomId={RoomId}", request.RoomId);
            return (new JoinGameResponse(), ReturnCode.RoomNotFound);
        }

        if (_playerManager.GetByPeer(peer) != null)
        {
            Log.Warning("[GameRoomManager] 加入游戏失败：连接已在游戏房间中 endpoint={EndPoint}", peer.Address);
            return (new JoinGameResponse(), ReturnCode.AlreadyInRoom);
        }

        var userId = request.Player.UserId;
        if (room.PlayerIds.Contains(userId))
        {
            Log.Warning("[GameRoomManager] 加入游戏失败：玩家已在房间中 userId={UserId} roomId={RoomId}",
                userId, request.RoomId);
            return (new JoinGameResponse(), ReturnCode.AlreadyInRoom);
        }

        var player = _playerManager.Add(peer, request.Player);
        player.CurrentRoomId = request.RoomId;
        room.PlayerIds.Add(userId);

        var notify = new JoinGameNotify { RoomId = request.RoomId, Player = player.Info };
        foreach (var otherId in room.PlayerIds.Where(id => id != userId))
        {
            var other = _playerManager.Get(otherId);
            if (other != null)
                MessageHelper.Send(other.Peer, MessageIds.JoinGameNotify, ReturnCode.Success, notify);
        }

        Log.Information("[GameRoomManager] 玩家加入游戏房间 roomId={RoomId} userId={UserId} 当前人数={Count}",
            request.RoomId, userId, room.PlayerIds.Count);

        return (new JoinGameResponse
        {
            RoomId = request.RoomId,
            RoomType = room.RoomType,
            OwnerUserId = room.OwnerUserId,
            Players = room.PlayerIds
                .Select(id => _playerManager.Get(id))
                .Where(p => p != null)
                .Select(p => p!.Info)
                .ToList()
        }, ReturnCode.Success);
    }

    public (ReturnCode Code, string? RoomId) LeaveGame(NetPeer peer)
    {
        return LeaveCore(peer, "离开");
    }

    public void RemovePlayer(NetPeer peer)
    {
        LeaveCore(peer, "断线离开");
    }

    private (ReturnCode Code, string? RoomId) LeaveCore(NetPeer peer, string reason)
    {
        var player = _playerManager.GetByPeer(peer);
        if (player == null || player.CurrentRoomId == null)
            return (ReturnCode.NotInRoom, null);

        var roomId = player.CurrentRoomId;
        if (!_rooms.TryGetValue(roomId, out var room))
            return (ReturnCode.NotInRoom, null);

        room.PlayerIds.Remove(player.Info.UserId);
        _playerManager.Remove(player.Info.UserId);

        var notify = new LeaveGameNotify { UserId = player.Info.UserId };
        foreach (var otherId in room.PlayerIds)
        {
            var other = _playerManager.Get(otherId);
            if (other != null)
                MessageHelper.Send(other.Peer, MessageIds.LeaveGameNotify, ReturnCode.Success, notify);
        }

        Log.Information("[GameRoomManager] 玩家{Reason}游戏房间 roomId={RoomId} 剩余人数={Count}",
            reason, roomId, room.PlayerIds.Count);

        if (room.PlayerIds.Count == 0)
        {
            _rooms.TryRemove(roomId, out _);
            Log.Information("[GameRoomManager] 游戏房间关闭 roomId={RoomId}", roomId);
        }

        return (ReturnCode.Success, roomId);
    }

    public void BroadcastToRoom(string roomId, NetPeer sender, ushort messageId, object data,
        DeliveryMethod method = DeliveryMethod.ReliableOrdered)
    {
        if (!_rooms.TryGetValue(roomId, out var room)) return;

        var frame = MessageHelper.CreateFrame(messageId, ReturnCode.Success, data);
        var peers = room.PlayerIds
            .Select(id => _playerManager.Get(id))
            .Where(p => p != null && p.Peer != sender)
            .Select(p => p!.Peer);
        MessageHelper.SendToAll(peers, frame, method);
    }

    public string? GetRoomId(NetPeer peer)
    {
        return _playerManager.GetByPeer(peer)?.CurrentRoomId;
    }
}
```

- [ ] **Step 5: 新增关闭 handler 并注册**

Create `GameServer/Handlers/CloseGameRoomHandler.cs`:

```csharp
using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using GameServer.Room;

namespace GameServer.Handlers;

public class CloseGameRoomHandler : MessageHandler<CloseGameRoomRequest>
{
    public override ushort MessageId => MessageIds.CloseGameRoom;

    private readonly GameRoomManager _roomManager;

    public CloseGameRoomHandler(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public override void HandleMessage(NetPeer peer, CloseGameRoomRequest request)
    {
        _roomManager.CloseRoom(request);
    }
}
```

Delete `GameServer/Handlers/AuthorizeGamePlayerHandler.cs`:

```bash
rm GameServer/Handlers/AuthorizeGamePlayerHandler.cs
```

Modify `GameServer/GameServer.cs` 的 `_lobbyRegistry.Register`：

```csharp
_lobbyRegistry.Register(
    new CreateGameRoomHandler(_roomManager),
    new CloseGameRoomHandler(_roomManager)
);
```

- [ ] **Step 6: 运行测试确认通过**

Run: `dotnet test GameServer.Tests --nologo --filter FullyQualifiedName~GameRoom`
Expected: 全部 PASS。

- [ ] **Step 7: 跑全量测试并提交**

Run: `dotnet test GameServer.Tests --nologo`
Expected: 全部 PASS。

Run: `dotnet build GameServer.sln`
Expected: 0 警告 0 错误。

```bash
git add -A GameServer GameServer.Tests
git commit -m "重构游戏房间生命周期"
```

---

## Task 4: 删除准备/开始/授权残留并更新客户端与文档

**Files:**
- Modify: `SharedLib/Protocol/MessageIds.cs`
- Modify: `SharedLib/Models/CreateGameRoomRequest.cs`
- Modify: `SharedLib/Models/JoinGameResponse.cs`
- Delete: `SharedLib/Models/GameReadyRequest.cs`
- Delete: `SharedLib/Models/GameReadyResponse.cs`
- Delete: `SharedLib/Models/GameReadyNotify.cs`
- Delete: `SharedLib/Models/GameUnreadyResponse.cs`
- Delete: `SharedLib/Models/GameStartRequest.cs`
- Delete: `SharedLib/Models/GameStartNotify.cs`
- Delete: `SharedLib/Models/AuthorizeGamePlayerRequest.cs`
- Modify: `LobbyServer/Room/RoomManager.cs`
- Modify: `GameServer/Room/GameRoom.cs`
- Modify: `GameServer/Room/GameRoomManager.cs`
- Modify: `TestClient/Program.cs`
- Modify: `AGENTS.md`

**Interfaces:**
- Consumes: Task 1–3 的全部改动
- Produces: 最终协议（`CreateGameRoom=102`）、`GameRoom` 不含房主、`JoinGameResponse` 不含房主、TestClient 直接 JoinGame

- [ ] **Step 1: 删除旧模型与消息 ID**

```bash
rm SharedLib/Models/GameReadyRequest.cs
rm SharedLib/Models/GameReadyResponse.cs
rm SharedLib/Models/GameReadyNotify.cs
rm SharedLib/Models/GameUnreadyResponse.cs
rm SharedLib/Models/GameStartRequest.cs
rm SharedLib/Models/GameStartNotify.cs
rm SharedLib/Models/AuthorizeGamePlayerRequest.cs
```

Replace `SharedLib/Protocol/MessageIds.cs`:

```csharp
namespace SharedLib.Protocol
{
    /// <summary>
    /// 消息 ID 常量
    /// </summary>
    public static class MessageIds
    {
        public const ushort JoinLobby = 1;
        public const ushort LeaveLobby = 2;
        public const ushort Chat = 3;
        public const ushort ChatNotify = 4;

        // 房间
        public const ushort CreateRoom = 10;
        public const ushort JoinRoom = 11;
        public const ushort LeaveRoom = 12;
        public const ushort JoinRoomNotify = 13;
        public const ushort LeaveRoomNotify = 14;
        public const ushort RoomList = 15;
        public const ushort RoomOwnerChangedNotify = 16;
        public const ushort RoomClosedNotify = 17;

        // 游戏房间（GameServer 侧）
        public const ushort JoinGame = 30;
        public const ushort LeaveGame = 31;
        public const ushort JoinGameNotify = 32;
        public const ushort LeaveGameNotify = 33;

        // GameServer 内部通信
        public const ushort GameServerRegister = 100;
        public const ushort GameServerUpdate = 101;
        public const ushort CreateGameRoom = 102;
        public const ushort CloseGameRoom = 103;

        // 同步
        public const ushort PositionSync = 40;
        public const ushort AnimationSync = 41;
        public const ushort ObjectSpawn = 42;
        public const ushort ObjectDespawn = 43;
    }
}
```

- [ ] **Step 2: 移除 GameServer 侧的房主字段**

Replace `SharedLib/Models/CreateGameRoomRequest.cs`:

```csharp
using MessagePack;

namespace SharedLib.Models;

[MessagePackObject]
public class CreateGameRoomRequest
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public RoomType RoomType { get; set; }
    [Key(2)] public int MaxPlayers { get; set; }
}
```

Replace `SharedLib/Models/JoinGameResponse.cs`:

```csharp
using MessagePack;

namespace SharedLib.Models;

[MessagePackObject]
public class JoinGameResponse
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public RoomType RoomType { get; set; }
    [Key(2)] public List<PlayerInfo> Players { get; set; } = [];
}
```

Replace `GameServer/Room/GameRoom.cs`:

```csharp
using SharedLib.Models;

namespace GameServer.Room;

public class GameRoom
{
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int MaxPlayers { get; set; }
    public HashSet<long> PlayerIds { get; set; } = [];
}
```

- [ ] **Step 3: 更新双方构造逻辑**

在 `LobbyServer/Room/RoomManager.cs` 的 `CreateRoom` 中，把 `CreateGameRoomRequest` 初始化改为：

```csharp
new CreateGameRoomRequest
{
    RoomId = roomId,
    RoomType = request.RoomType,
    MaxPlayers = request.MaxPlayers
}
```

在 `GameServer/Room/GameRoomManager.cs` 的 `CreateRoom` 中，把 `GameRoom` 初始化改为：

```csharp
var room = new GameRoom
{
    RoomId = request.RoomId,
    RoomType = request.RoomType,
    MaxPlayers = request.MaxPlayers
};
```

在同文件 `JoinGame` 的返回值中删除 `OwnerUserId = room.OwnerUserId,` 这一行。

- [ ] **Step 4: 更新 TestClient**

Modify `TestClient/Program.cs`：

1. 删除 `GameReady`、`GameUnready`、`GameStart`、`GameStartNotify` 四个 `case` 分支。
2. 在大厅消息 `switch` 中加入：

```csharp
case MessageIds.RoomOwnerChangedNotify:
    var ownerNotify = MessageHelper.Deserialize<RoomOwnerChangedNotify>(frame);
    Log.Information("[TestClient] 房主变更 roomId={RoomId} owner={Owner}",
        ownerNotify?.RoomId, ownerNotify?.OwnerUserId);
    break;

case MessageIds.RoomClosedNotify:
    var closedNotify = MessageHelper.Deserialize<RoomClosedNotify>(frame);
    Log.Information("[TestClient] 房间已关闭 roomId={RoomId} reason={Reason}",
        closedNotify?.RoomId, closedNotify?.Reason);
    gameRoomId = null;
    break;
```

3. 在 `CreateRoom` 成功分支加入：

```csharp
if (createRes?.Room != null)
    ConnectGameServer(createRes.Room);
```

4. 在 `JoinRoom` 成功分支加入：

```csharp
if (r != null)
    ConnectGameServer(r);
```

5. 在游戏服消息 `switch` 中加入：

```csharp
case MessageIds.RoomClosedNotify:
    var gameClosed = MessageHelper.Deserialize<RoomClosedNotify>(frame);
    Log.Information("[TestClient] 游戏房间已关闭 roomId={RoomId} reason={Reason}",
        gameClosed?.RoomId, gameClosed?.Reason);
    gameRoomId = null;
    break;
```

6. 把 `JoinGame` 成功日志改为：

```csharp
Log.Information("[TestClient] 游戏房间加入成功 roomId={RoomId}", joinGameRes?.RoomId);
```

7. 在 `SendGameMessage` 之后加入局部函数：

```csharp
void ConnectGameServer(RoomInfo room)
{
    gameRoomId = room.RoomId;
    if (gamePeer?.ConnectionState == ConnectionState.Connected)
    {
        SendGameMessage(MessageIds.JoinGame, new JoinGameRequest
        {
            RoomId = room.RoomId,
            Player = player
        });
        return;
    }

    gameClient.Connect(room.GameServerAddress, room.GameServerPort, ConnectionKey);
}
```

8. 删除命令分支 `ready`、`unready`、`start`，并把命令提示改为：

```csharp
Log.Information("[TestClient] 可用命令: joinlobby | leavelobby | chat <内容> | createroom [quick] [maxPlayers] [roomId] | joinroom <roomId> | leaveroom | rooms | joingame <roomId> | leavegame | status | quit");
```

- [ ] **Step 5: 更新 AGENTS.md**

在 `AGENTS.md` 的 `## 网络协议（新增消息时）` 一节末尾、现有四条「新增一条消息」之后，加入：

```markdown
房间相关内部消息：`CreateGameRoom = 102`、`CloseGameRoom = 103`；客户端通知：`RoomOwnerChangedNotify = 16`、`RoomClosedNotify = 17`。

大厅房间是组队房，没有准备/开始/授权流程：
- 玩家 `CreateRoom` 时自动入房并成为房主；外部/系统通过 `LobbyServer.CreateSystemRoom` 创建空房，第一个 `JoinRoom` 的玩家成为房主。
- 建房时 LobbyServer 立即向选中的 GameServer 发 `CreateGameRoom`（幂等）；房间空了或被 `LobbyServer.CloseRoom` 关闭时发 `CloseGameRoom`，GameServer 通知并移出仍在游戏内的玩家。
- 玩家拿到 `JoinRoomResponse` 里的 GameServer 地址后直接连游戏服发 `JoinGame`，GameServer 不校验授权。
- 房主离开时按 `PlayerIds` 加入顺序转移给下一个成员，并广播 `RoomOwnerChangedNotify`。
- `MaxPlayers` 只在 Lobby 层校验；GameServer 不限制人数。
```

然后修改 `## 约定`：

- 把「房间流程」条目中关于 `StartGame` / `GameServerOffline` 的句子删掉，保留 `PickGameServer` 按 `PlayerCount` 负载均衡、`GameServerRegistry` 以 `Address:Port` 为 key 的描述。
- 删除「房间类型：`Default` 房主开局……`QuickMatch` 自由加入……」整条。
- 删除「开局授权：`StartGame` 时 LobbyServer 发 `CreateGameRoom`……玩家必须先大厅加入→开始→才能进游戏服」整条。
- 把 `PlayerState` 描述改为 `InLobby/InRoom`。

- [ ] **Step 6: 全量验证**

Run: `dotnet build GameServer.sln`
Expected: 0 警告 0 错误。

Run: `dotnet test GameServer.Tests --nologo`
Expected: 全部 PASS（Task 1 + Task 2 + Task 3 的所有测试）。

- [ ] **Step 7: 提交**

```bash
git add -A
git commit -m "删除准备与开局流程并更新客户端文档"
```

---

## Task 5: 手动冒烟（可选但建议）

**Files:** 无新增

- [ ] **Step 1: 启动两个服务端**

```bash
dotnet run --project LobbyServer --no-build
```

开第二个终端：

```bash
dotnet run --project GameServer --no-build
```

- [ ] **Step 2: 启动两个 TestClient**

开两个终端分别运行：

```bash
dotnet run --project TestClient --no-build
```

在每个客户端依次执行：

```text
joinlobby
createroom
```

第一个客户端创建房间后，第二个客户端执行：

```text
joinlobby
joinroom <第一个客户端日志里的 roomId>
```

- [ ] **Step 3: 验证直接进游戏**

两个客户端都应在 `CreateRoom` / `JoinRoom` 成功后自动连接 GameServer 并打印「游戏房间加入成功」。第二个客户端加入大厅房间时，第一个客户端应收到 `JoinRoomNotify`；房主离开时其余客户端应打印「房主变更」。

- [ ] **Step 4: 清理**

在两个终端按 `Ctrl+C` 停止服务端；确认没有遗留 `dotnet` 进程（`ps aux | grep -E 'LobbyServer|GameServer|TestClient'`）。
