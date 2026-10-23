# Redis 房间持久化与重启恢复实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 LobbyServer 的房间/成员/房主持久化到 Redis，使 Lobby 重启后能恢复房间，GameServer 重启后由 Lobby 自动补建游戏房间。

**Architecture:** Redis 只归档 LobbyRoom（Hash: `{prefix}:lobby:rooms`）。Lobby 启动时恢复房间和离线成员；玩家重连后绑定 peer。GameServer 不写 Redis，注册时由 `RoomManager.ResyncGameServer` 按 `Address:Port` 补发 `CreateGameRoom`（幂等）。

**Tech Stack:** .NET 10、LiteNetLib 2.1.4、MessagePack 3.1.7、Newtonsoft.Json 13.0.4、StackExchange.Redis 3.4.5、Serilog、xunit 2.9.3

**Spec:** `docs/superpowers/specs/2026-10-23-redis-room-persistence-design.md`

## Global Constraints

- 目标框架 `net10.0`；`dotnet build GameServer.sln` 必须 0 警告 0 错误。
- 只新增一个 NuGet 依赖：`StackExchange.Redis` 3.4.5。
- 只持久化 LobbyRoom；GameServer 不写 Redis。
- Redis key：`{prefix}:lobby:rooms`（Hash，field=roomId，value=JSON）。
- 房间不设 TTL；关房/空房时显式 `HDEL`。
- Redis 启动失败：Lobby 退出；运行期写失败：记录日志，内存继续。
- 单 LobbyServer 实例；不做多实例锁。
- 服务端注释和日志使用中文。
- 现有 27 个测试必须继续通过；默认 `ServerFixture` 使用内存存储，不依赖 Redis。
- Redis 集成测试使用唯一 key 前缀，结束时清理。
- 不修改现有协议消息；`CreateGameRoom` 复用。

## Review Focus

- Lobby 重启后房间不能被“空房规则”误删，成员/房主必须保留；重新 `JoinLobby` 后不能出现重复成员。测试：`LobbyRestart_RestoresRoomsAndMembers`（Task 5）。
- GameServer 重启后，Lobby 必须在 GS 注册时补建房间；补建必须幂等，不能清空 GS 已有成员。测试：`GameServerRestart_RecreatesGameRooms`（Task 5）。
- 恢复出的空玩家房必须删除，空系统房必须保留；房主不在成员列表时回退到第一个成员。测试：`RoomManagerRestoreTests`（Task 4）。
- 同一 userId 重连/顶号时必须保留房间身份，旧 peer 的断开事件不能把新会话移出房间。测试：`JoinLobby_Twice_KeepsRoomMembership`、`Takeover_KeepsRoomMembership`（Task 3）。
- Redis 运行期写失败不能把服务打挂（启动失败则是致命的）。测试：`Save_WhenStoreDisposed_DoesNotThrow`（Task 2）。

---

## Task 1: RedisHelper、依赖与配置

**Files:**
- Modify: `SharedLib/SharedLib.csproj`
- Create: `SharedLib/Redis/RedisHelper.cs`
- Modify: `LobbyServer/LobbyServerConfig.cs`
- Modify: `LobbyServer/LobbyServerConfig.json`
- Test: `GameServer.Tests/RedisHelperTests.cs`

**Interfaces:**
- Produces: `RedisHelper(string connectionString, int database = 0)`、`Ping()`、`HashGet<T>`、`HashGetAll<T>`、`HashSet<T>`、`HashDelete`、`Delete`、`ScanKeys`、`Database`
- Produces: `LobbyServerConfig.RedisConnection/RedisKeyPrefix/RedisDatabase`

- [ ] **Step 1: 写失败的 RedisHelper 测试**

Create `GameServer.Tests/RedisHelperTests.cs`:

```csharp
using SharedLib.Redis;

namespace GameServer.Tests;

public class RedisHelperTests : IDisposable
{
    private readonly string _prefix = $"test:redis:{Guid.NewGuid():N}";
    private readonly RedisHelper _redis = new("localhost:6379");

    [Fact]
    public void HashSetGetGetAllDelete_RoundTrip()
    {
        var key = $"{_prefix}:hash";

        Assert.True(_redis.HashSet(key, "a", new TestValue { Value = 1 }));
        Assert.True(_redis.HashSet(key, "b", new TestValue { Value = 2 }));

        var a = _redis.HashGet<TestValue>(key, "a");
        Assert.NotNull(a);
        Assert.Equal(1, a!.Value);

        var all = _redis.HashGetAll<TestValue>(key);
        Assert.Equal(2, all.Count);
        Assert.Equal(2, all["b"].Value);

        Assert.True(_redis.HashDelete(key, "a"));
        Assert.Null(_redis.HashGet<TestValue>(key, "a"));
        Assert.True(_redis.Delete(key));
        Assert.Empty(_redis.HashGetAll<TestValue>(key));
    }

    [Fact]
    public void ScanKeys_FindsKeysByPattern()
    {
        var key = $"{_prefix}:scan:1";
        _redis.HashSet(key, "f", new TestValue { Value = 1 });

        var keys = _redis.ScanKeys($"{_prefix}:scan:*").ToList();

        Assert.Contains(key, keys);
    }

    [Fact]
    public void Ping_ReturnsTrue()
    {
        Assert.True(_redis.Ping());
    }

    public void Dispose()
    {
        foreach (var key in _redis.ScanKeys($"{_prefix}:*"))
            _redis.Delete(key);
        _redis.Dispose();
    }

    public sealed class TestValue
    {
        public int Value { get; set; }
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test GameServer.Tests --nologo --filter FullyQualifiedName~RedisHelperTests`
Expected: 编译失败，提示 `SharedLib.Redis` / `RedisHelper` 不存在。

- [ ] **Step 3: 添加依赖与 RedisHelper**

Modify `SharedLib/SharedLib.csproj`，在 `ItemGroup` 中加入：

```xml
<PackageReference Include="StackExchange.Redis" Version="3.4.5"/>
```

Create `SharedLib/Redis/RedisHelper.cs`:

```csharp
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;

namespace SharedLib.Redis;

/// <summary>
/// Redis 连接与通用读写封装
/// </summary>
public sealed class RedisHelper : IDisposable
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly ConnectionMultiplexer _connection;
    private readonly IDatabase _database;

    public RedisHelper(string connectionString, int database = 0)
    {
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = true;
        options.ConnectTimeout = 5000;

        _connection = ConnectionMultiplexer.Connect(options);
        _database = _connection.GetDatabase(database);
    }

    public IDatabase Database => _database;

    public bool Ping()
    {
        try
        {
            _database.Ping();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Redis] Ping 失败");
            return false;
        }
    }

    public T? HashGet<T>(string key, string field)
    {
        try
        {
            var value = _database.HashGet(key, field);
            if (value.IsNullOrEmpty)
                return default;

            return JsonConvert.DeserializeObject<T>(value.ToString(), JsonSettings);
        }
        catch (JsonException ex)
        {
            Log.Warning(ex, "[Redis] 反序列化失败 key={Key} field={Field}", key, field);
            return default;
        }
    }

    public IReadOnlyDictionary<string, T> HashGetAll<T>(string key)
    {
        var result = new Dictionary<string, T>();

        foreach (var entry in _database.HashGetAll(key))
        {
            if (entry.Value.IsNullOrEmpty)
                continue;

            try
            {
                var value = JsonConvert.DeserializeObject<T>(entry.Value.ToString(), JsonSettings);
                if (value != null)
                    result[entry.Name.ToString()] = value;
            }
            catch (JsonException ex)
            {
                Log.Warning(ex, "[Redis] 反序列化失败 key={Key} field={Field}", key, entry.Name.ToString());
            }
        }

        return result;
    }

    public bool HashSet<T>(string key, string field, T value)
    {
        var json = JsonConvert.SerializeObject(value, JsonSettings);
        return _database.HashSet(key, field, json);
    }

    public bool HashDelete(string key, string field)
    {
        return _database.HashDelete(key, field);
    }

    public bool Delete(string key)
    {
        return _database.KeyDelete(key);
    }

    public IEnumerable<string> ScanKeys(string pattern)
    {
        var endpoint = _connection.GetEndPoints().FirstOrDefault();
        if (endpoint == null)
            yield break;

        foreach (var key in _connection.GetServer(endpoint).Keys(pattern: pattern, pageSize: 250))
            yield return key.ToString();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
```

- [ ] **Step 4: 增加 Redis 配置项**

Modify `LobbyServer/LobbyServerConfig.cs`，加入：

```csharp
public string RedisConnection { get; set; } = "localhost:6379";
public string RedisKeyPrefix { get; set; } = "gameserver";
public int RedisDatabase { get; set; } = 0;
```

Modify `LobbyServer/LobbyServerConfig.json`：

```json
{
  "ClientPort": 6001,
  "ServerPort": 6002,
  "ClientConnectionKey": "Client@wasd9527",
  "ServerConnectionKey": "Game@wasd9527",
  "UpdateTime": 15,
  "PingInterval": 1000,
  "DisconnectTimeout": 5000,
  "ChannelsCount": 1,
  "RedisConnection": "localhost:6379",
  "RedisKeyPrefix": "gameserver",
  "RedisDatabase": 0
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test GameServer.Tests --nologo --filter FullyQualifiedName~RedisHelperTests`
Expected: PASS（3 个测试）。

- [ ] **Step 6: 编译并提交**

Run: `dotnet build GameServer.sln`
Expected: 0 警告 0 错误。

```bash
git add SharedLib/SharedLib.csproj SharedLib/Redis/RedisHelper.cs \
  LobbyServer/LobbyServerConfig.cs LobbyServer/LobbyServerConfig.json \
  GameServer.Tests/RedisHelperTests.cs
git commit -m "新增Redis工具类与配置"
```

---

## Task 2: 房间快照与存储层

**Files:**
- Create: `LobbyServer/Persistence/LobbyRoomSnapshot.cs`
- Create: `LobbyServer/Persistence/ILobbyRoomStore.cs`
- Create: `LobbyServer/Persistence/RedisLobbyRoomStore.cs`
- Create: `GameServer.Tests/InMemoryLobbyRoomStore.cs`
- Test: `GameServer.Tests/LobbyRoomStoreTests.cs`

**Interfaces:**
- Consumes: Task 1 的 `RedisHelper`
- Produces:
  - `LobbyRoomSnapshot { Version, RoomId, RoomType, MaxPlayers, OwnerUserId, CreatorUserId, GameServerAddress, GameServerPort, List<PlayerInfo> Players }`
  - `ILobbyRoomStore : IDisposable { LoadAll(); Save(snapshot); Delete(roomId); }`
  - `RedisLobbyRoomStore(string connectionString, string keyPrefix, int database = 0)`
  - `InMemoryLobbyRoomStore`（测试项目）

- [ ] **Step 1: 写失败的存储层测试**

Create `GameServer.Tests/LobbyRoomStoreTests.cs`:

```csharp
using LobbyServer.Persistence;
using SharedLib.Models;
using SharedLib.Redis;

namespace GameServer.Tests;

public class LobbyRoomStoreTests : IDisposable
{
    private readonly string _prefix = $"test:roomstore:{Guid.NewGuid():N}";
    private readonly RedisHelper _redis = new("localhost:6379");
    private readonly RedisLobbyRoomStore _store;

    public LobbyRoomStoreTests()
    {
        _store = new RedisLobbyRoomStore("localhost:6379", _prefix);
    }

    [Fact]
    public void SaveLoadDelete_RoundTrip()
    {
        var snapshot = BuildSnapshot("room-1");

        _store.Save(snapshot);

        var loaded = _store.LoadAll().Single();
        Assert.Equal("room-1", loaded.RoomId);
        Assert.Equal(4, loaded.MaxPlayers);
        Assert.Equal(42L, loaded.OwnerUserId);
        Assert.Equal("127.0.0.1", loaded.GameServerAddress);
        Assert.Equal(2, loaded.Players.Count);
        Assert.Equal(42L, loaded.Players[0].UserId);
        Assert.Equal(43L, loaded.Players[1].UserId);

        _store.Delete("room-1");
        Assert.Empty(_store.LoadAll());
    }

    [Fact]
    public void LoadAll_SkipsCorruptJson()
    {
        var key = $"{_prefix}:lobby:rooms";
        _redis.Database.HashSet(key, "bad", "{not-json");

        Assert.Empty(_store.LoadAll());
    }

    [Fact]
    public void Save_WhenStoreDisposed_DoesNotThrow()
    {
        _store.Dispose();

        var exception = Record.Exception(() => _store.Save(BuildSnapshot("room-after-dispose")));

        Assert.Null(exception);
    }

    private static LobbyRoomSnapshot BuildSnapshot(string roomId)
    {
        return new LobbyRoomSnapshot
        {
            RoomId = roomId,
            RoomType = RoomType.Default,
            MaxPlayers = 4,
            OwnerUserId = 42,
            CreatorUserId = 42,
            GameServerAddress = "127.0.0.1",
            GameServerPort = 7001,
            Players =
            [
                new PlayerInfo { UserId = 42, Nickname = "owner" },
                new PlayerInfo { UserId = 43, Nickname = "member" }
            ]
        };
    }

    public void Dispose()
    {
        foreach (var key in _redis.ScanKeys($"{_prefix}:*"))
            _redis.Delete(key);
        _store.Dispose();
        _redis.Dispose();
    }
}
```

Create `GameServer.Tests/InMemoryLobbyRoomStore.cs`:

```csharp
using LobbyServer.Persistence;
using SharedLib.Models;

namespace GameServer.Tests;

public sealed class InMemoryLobbyRoomStore : ILobbyRoomStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, LobbyRoomSnapshot> _rooms = new();

    public IReadOnlyList<LobbyRoomSnapshot> LoadAll()
    {
        lock (_gate)
            return _rooms.Values.Select(Clone).ToList();
    }

    public void Save(LobbyRoomSnapshot room)
    {
        lock (_gate)
            _rooms[room.RoomId] = Clone(room);
    }

    public void Delete(string roomId)
    {
        lock (_gate)
            _rooms.Remove(roomId);
    }

    public void Dispose()
    {
    }

    private static LobbyRoomSnapshot Clone(LobbyRoomSnapshot room)
    {
        return new LobbyRoomSnapshot
        {
            Version = room.Version,
            RoomId = room.RoomId,
            RoomType = room.RoomType,
            MaxPlayers = room.MaxPlayers,
            OwnerUserId = room.OwnerUserId,
            CreatorUserId = room.CreatorUserId,
            GameServerAddress = room.GameServerAddress,
            GameServerPort = room.GameServerPort,
            Players = room.Players.Select(p => new PlayerInfo
            {
                UserId = p.UserId,
                Nickname = p.Nickname,
                Gender = p.Gender,
                Avatar = p.Avatar,
                Age = p.Age,
                ExtraData = new Dictionary<string, string>(p.ExtraData ?? new Dictionary<string, string>())
            }).ToList()
        };
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test GameServer.Tests --nologo --filter FullyQualifiedName~LobbyRoomStoreTests`
Expected: 编译失败，提示 `LobbyServer.Persistence` / `LobbyRoomSnapshot` / `RedisLobbyRoomStore` 不存在。

- [ ] **Step 3: 新增快照与存储接口**

Create `LobbyServer/Persistence/LobbyRoomSnapshot.cs`:

```csharp
using SharedLib.Models;

namespace LobbyServer.Persistence;

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
    public List<PlayerInfo> Players { get; set; } = [];
}
```

Create `LobbyServer/Persistence/ILobbyRoomStore.cs`:

```csharp
namespace LobbyServer.Persistence;

public interface ILobbyRoomStore : IDisposable
{
    IReadOnlyList<LobbyRoomSnapshot> LoadAll();
    void Save(LobbyRoomSnapshot room);
    void Delete(string roomId);
}
```

- [ ] **Step 4: 实现 Redis 存储**

Create `LobbyServer/Persistence/RedisLobbyRoomStore.cs`:

```csharp
using Serilog;
using SharedLib.Redis;

namespace LobbyServer.Persistence;

public sealed class RedisLobbyRoomStore : ILobbyRoomStore
{
    private readonly RedisHelper _redis;
    private readonly string _key;
    private bool _disposed;

    public RedisLobbyRoomStore(string connectionString, string keyPrefix, int database = 0)
    {
        _redis = new RedisHelper(connectionString, database);
        _key = $"{keyPrefix}:lobby:rooms";
    }

    public IReadOnlyList<LobbyRoomSnapshot> LoadAll()
    {
        return _redis.HashGetAll<LobbyRoomSnapshot>(_key).Values.ToList();
    }

    public void Save(LobbyRoomSnapshot room)
    {
        try
        {
            _redis.HashSet(_key, room.RoomId, room);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Redis] 保存房间失败 roomId={RoomId}", room.RoomId);
        }
    }

    public void Delete(string roomId)
    {
        try
        {
            _redis.HashDelete(_key, roomId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Redis] 删除房间失败 roomId={RoomId}", roomId);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _redis.Dispose();
    }
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test GameServer.Tests --nologo --filter FullyQualifiedName~LobbyRoomStoreTests`
Expected: PASS（3 个测试）。

- [ ] **Step 6: 编译并提交**

Run: `dotnet build GameServer.sln`
Expected: 0 警告 0 错误。

```bash
git add LobbyServer/Persistence GameServer.Tests/InMemoryLobbyRoomStore.cs \
  GameServer.Tests/LobbyRoomStoreTests.cs
git commit -m "新增房间快照与Redis存储层"
```

---

## Task 3: 离线成员、重连绑定与广播过滤

**Files:**
- Modify: `LobbyServer/Player/LobbyPlayer.cs`
- Modify: `LobbyServer/Player/PlayerManager.cs`
- Modify: `LobbyServer/Lobby/LobbyManager.cs`
- Modify: `LobbyServer/Room/RoomManager.cs`
- Test: `GameServer.Tests/PlayerManagerTests.cs`
- Modify: `GameServer.Tests/LobbyFlowTests.cs`

**Interfaces:**
- Produces:
  - `LobbyPlayer.Peer` 变为 `NetPeer?`
  - `PlayerManager.AddOffline(PlayerInfo, string roomId) : LobbyPlayer`
  - `PlayerManager.BindOnline(LobbyPlayer, NetPeer, PlayerInfo) : void`
  - `PlayerManager.UnbindPeer(NetPeer) : void`
  - `PlayerManager.OnlinePeers : IEnumerable<NetPeer>`
  - `PlayerManager.IsOnline(long) : bool`
  - `LobbyManager.Join` 对已存在 userId 保留房间身份
- Consumes: 无（本任务不接持久化）

- [ ] **Step 1: 写失败的离线成员/重连测试**

Create `GameServer.Tests/PlayerManagerTests.cs`:

```csharp
using LobbyServer.Player;
using SharedLib.Models;

namespace GameServer.Tests;

public class PlayerManagerTests
{
    [Fact]
    public void AddOffline_CreatesMemberWithoutPeer()
    {
        var manager = new PlayerManager();

        var player = manager.AddOffline(new PlayerInfo { UserId = 42, Nickname = "offline" }, "room-1");

        Assert.Null(player.Peer);
        Assert.Equal(PlayerState.InRoom, player.State);
        Assert.Equal("room-1", player.CurrentRoomId);
        Assert.False(manager.IsOnline(42));
        Assert.Empty(manager.OnlinePeers);
        Assert.Single(manager.All);
    }

    [Fact]
    public void Remove_OfflinePlayer_DoesNotThrow()
    {
        var manager = new PlayerManager();
        manager.AddOffline(new PlayerInfo { UserId = 42 }, "room-1");

        Assert.True(manager.Remove(42));
        Assert.Null(manager.Get(42));
    }
}
```

Modify `GameServer.Tests/LobbyFlowTests.cs` 的 `JoinLobby_Twice_RemovesPreviousRoomMembership`，改为验证“保留成员身份”：

```csharp
    [Fact]
    public void JoinLobby_Twice_KeepsRoomMembership()
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
        Assert.Equal(3, joined.Room.Players.Count);
        Assert.Equal(1801L, joined.Room.OwnerUserId);
    }

    [Fact]
    public void Takeover_KeepsRoomMembership()
    {
        using var owner = ConnectClient(1851, "owner");
        CreateRoom(owner, "room-takeover");

        using var replacement = new TestClient();
        replacement.Connect(_fixture.LobbyClientPort, ServerFixture.ClientKey);
        Assert.True(replacement.WaitConnected());
        replacement.Send(MessageIds.JoinLobby, new JoinLobbyRequest
        {
            Player = new PlayerInfo { UserId = 1851, Nickname = "owner-new" }
        });
        Assert.NotNull(replacement.WaitPayload<JoinLobbyResponse>(MessageIds.JoinLobby));

        using var checker = ConnectClient(1852, "checker");
        var joined = JoinRoom(checker, "room-takeover");

        Assert.Equal(2, joined.Room.Players.Count);
        Assert.Equal(1851L, joined.Room.OwnerUserId);
        Assert.Contains(joined.Room.Players, p => p.UserId == 1851 && p.Nickname == "owner-new");
    }
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test GameServer.Tests --nologo --filter "FullyQualifiedName~PlayerManagerTests|FullyQualifiedName~JoinLobby_Twice|FullyQualifiedName~Takeover"`
Expected: 编译失败（`AddOffline`/`IsOnline`/`OnlinePeers` 不存在），或行为测试失败。

- [ ] **Step 3: LobbyPlayer.Peer 改为可空**

Replace `LobbyServer/Player/LobbyPlayer.cs`:

```csharp
using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Player;

public class LobbyPlayer
{
    public PlayerInfo Info { get; set; } = new();
    public NetPeer? Peer { get; set; }
    public PlayerState State { get; set; } = PlayerState.InLobby;
    public string? CurrentRoomId { get; set; }
}
```

- [ ] **Step 4: PlayerManager 增加离线/绑定支持**

Replace `LobbyServer/Player/PlayerManager.cs`:

```csharp
using System.Collections.Concurrent;
using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Player;

public class PlayerManager
{
    private readonly ConcurrentDictionary<long, LobbyPlayer> _players = new();
    private readonly ConcurrentDictionary<NetPeer, long> _peerToUserId = new();

    public int Count => _players.Count;

    public IEnumerable<LobbyPlayer> All => _players.Values;

    public IEnumerable<NetPeer> OnlinePeers
    {
        get
        {
            return _players.Values
                .Where(p => p.Peer is { ConnectionState: ConnectionState.Connected })
                .Select(p => p.Peer!);
        }
    }

    public LobbyPlayer? Get(long userId)
    {
        _players.TryGetValue(userId, out var player);
        return player;
    }

    public LobbyPlayer? GetByPeer(NetPeer peer)
    {
        return _peerToUserId.TryGetValue(peer, out var userId) ? Get(userId) : null;
    }

    public long GetUserId(NetPeer peer)
    {
        _peerToUserId.TryGetValue(peer, out var userId);
        return userId;
    }

    public LobbyPlayer Add(NetPeer peer, PlayerInfo info)
    {
        var player = new LobbyPlayer
        {
            Info = info,
            Peer = peer,
            State = PlayerState.InLobby
        };

        _players[info.UserId] = player;
        _peerToUserId[peer] = info.UserId;

        return player;
    }

    public LobbyPlayer AddOffline(PlayerInfo info, string roomId)
    {
        var player = new LobbyPlayer
        {
            Info = info,
            Peer = null,
            State = PlayerState.InRoom,
            CurrentRoomId = roomId
        };

        _players[info.UserId] = player;
        return player;
    }

    public void BindOnline(LobbyPlayer player, NetPeer peer, PlayerInfo info)
    {
        player.Peer = peer;
        player.Info = info;
        _peerToUserId[peer] = player.Info.UserId;
    }

    public void UnbindPeer(NetPeer peer)
    {
        if (_peerToUserId.TryRemove(peer, out var userId) &&
            _players.TryGetValue(userId, out var player) &&
            player.Peer == peer)
        {
            player.Peer = null;
        }
    }

    public bool Remove(long userId)
    {
        if (!_players.TryRemove(userId, out var player))
            return false;

        if (player.Peer != null)
            _peerToUserId.TryRemove(player.Peer, out _);

        return true;
    }

    public void RemoveByPeer(NetPeer peer)
    {
        if (_peerToUserId.TryRemove(peer, out var userId))
            _players.TryRemove(userId, out _);
    }

    public void SetState(long userId, PlayerState state, string? roomId = null)
    {
        if (_players.TryGetValue(userId, out var player))
        {
            player.State = state;
            player.CurrentRoomId = roomId ?? (state == PlayerState.InLobby ? null : player.CurrentRoomId);
        }
    }

    public PlayerState GetState(long userId)
    {
        return _players.TryGetValue(userId, out var player) ? player.State : PlayerState.InLobby;
    }

    public bool IsOnline(long userId)
    {
        return _players.TryGetValue(userId, out var player) &&
               player.Peer is { ConnectionState: ConnectionState.Connected };
    }
}
```

- [ ] **Step 5: LobbyManager 的重连/顶号规则与广播过滤**

Modify `LobbyManager.Join`：

```csharp
public (JoinLobbyResponse Response, ReturnCode Code) Join(NetPeer peer, JoinLobbyRequest request)
{
    var userId = request.Player.UserId;
    Log.Information("[LobbyManager] 加入大厅 userId={UserId}", userId);

    var existing = _players.Get(userId);
    if (existing != null)
    {
        Log.Warning("[LobbyManager] 玩家重新登录 userId={UserId} online={Online}", userId, existing.Peer != null);

        var oldPeer = existing.Peer;
        if (oldPeer != null && oldPeer != peer)
        {
            _players.UnbindPeer(oldPeer);
            _netManager.DisconnectPeer(oldPeer);
        }

        _players.BindOnline(existing, peer, request.Player);

        Log.Information("[LobbyManager] 玩家恢复会话 userId={UserId} roomId={RoomId}",
            userId, existing.CurrentRoomId);

        return (new JoinLobbyResponse { Player = request.Player }, ReturnCode.Success);
    }

    _players.Add(peer, request.Player);

    Log.Information("[LobbyManager] 大厅加入 userId={UserId} nickname={Nickname} 在线人数={Count}",
        userId, request.Player.Nickname, _players.Count);

    return (new JoinLobbyResponse { Player = request.Player }, ReturnCode.Success);
}
```

Modify `LobbyManager.Broadcast` 的最后一行：

```csharp
MessageHelper.SendToAll(_players.OnlinePeers, frame, DeliveryMethod.ReliableOrdered);
```

- [ ] **Step 6: RoomManager 过滤离线成员**

Modify `RoomManager.JoinRoom` 的通知循环：

```csharp
foreach (var otherId in room.PlayerIds.Where(id => id != userId))
{
    var otherPlayer = _players.Get(otherId);
    if (otherPlayer?.Peer is { ConnectionState: ConnectionState.Connected } otherPeer)
        MessageHelper.Send(otherPeer, MessageIds.JoinRoomNotify, ReturnCode.Success, notify);
}
```

Modify `RoomManager.LeaveRoom` 的两处循环（房主变更在 `Broadcast` 内；`LeaveRoomNotify` 循环）：

```csharp
foreach (var otherId in room.PlayerIds)
{
    var otherPlayer = _players.Get(otherId);
    if (otherPlayer?.Peer is { ConnectionState: ConnectionState.Connected } otherPeer)
        MessageHelper.Send(otherPeer, MessageIds.LeaveRoomNotify, ReturnCode.Success, leaveNotify);
}
```

Modify `RoomManager.Broadcast`：

```csharp
private void Broadcast(LobbyRoom room, ushort messageId, object data)
{
    var frame = MessageHelper.CreateFrame(messageId, ReturnCode.Success, data);
    var peers = room.PlayerIds
        .Select(id => _players.Get(id))
        .Where(p => p?.Peer is { ConnectionState: ConnectionState.Connected })
        .Select(p => p!.Peer!);

    MessageHelper.SendToAll(peers, frame, DeliveryMethod.ReliableOrdered);
}
```

Modify `RoomManager.CloseRoom` 收集 peer 的部分：

```csharp
if (player?.Peer is { ConnectionState: ConnectionState.Connected } playerPeer)
    peers.Add(playerPeer);

_players.SetState(userId, PlayerState.InLobby);
```

- [ ] **Step 7: 运行测试确认通过**

Run: `dotnet test GameServer.Tests --nologo --filter "FullyQualifiedName~PlayerManagerTests|FullyQualifiedName~JoinLobby_Twice|FullyQualifiedName~Takeover"`
Expected: PASS。

- [ ] **Step 8: 跑全量测试并提交**

Run: `dotnet test GameServer.Tests --nologo`
Expected: 全部 PASS。

Run: `dotnet build GameServer.sln`
Expected: 0 警告 0 错误。

```bash
git add LobbyServer/Player LobbyServer/Lobby/LobbyManager.cs LobbyServer/Room/RoomManager.cs \
  GameServer.Tests/PlayerManagerTests.cs GameServer.Tests/LobbyFlowTests.cs
git commit -m "支持离线房间成员与重连绑定"
```

---

## Task 4: RoomManager 恢复/持久化与 LobbyServer 接线

**Files:**
- Modify: `LobbyServer/Room/RoomManager.cs`
- Modify: `LobbyServer/Lobby/LobbyManager.cs`（重连后保存快照）
- Modify: `LobbyServer/LobbyServer.cs`
- Modify: `GameServer.Tests/ServerFixture.cs`
- Test: `GameServer.Tests/RoomManagerRestoreTests.cs`

**Interfaces:**
- Consumes: Task 2 的 `ILobbyRoomStore`、`LobbyRoomSnapshot`；Task 3 的离线成员
- Produces:
  - `RoomManager(PlayerManager, GameServerRegistry, ILobbyRoomStore)`
  - `RoomManager.RestoreFromStore() : void`
  - `RoomManager.PersistPlayerRoom(long userId) : void`
  - `RoomManager.GetRoom(string roomId) : LobbyRoom?`
  - `LobbyServer(LobbyServerConfig, ILobbyRoomStore? store = null)`

- [ ] **Step 1: 写失败的恢复测试**

Create `GameServer.Tests/RoomManagerRestoreTests.cs`:

```csharp
using LobbyServer.Cluster;
using LobbyServer.Persistence;
using LobbyServer.Player;
using LobbyServer.Room;
using SharedLib.Models;

namespace GameServer.Tests;

public class RoomManagerRestoreTests
{
    [Fact]
    public void Restore_RebuildsRoomsAndOfflineMembers()
    {
        var store = new InMemoryLobbyRoomStore();
        store.Save(new LobbyRoomSnapshot
        {
            RoomId = "room-1",
            RoomType = RoomType.Default,
            MaxPlayers = 4,
            OwnerUserId = 42,
            CreatorUserId = 42,
            GameServerAddress = "127.0.0.1",
            GameServerPort = 7001,
            Players =
            [
                new PlayerInfo { UserId = 42, Nickname = "owner" },
                new PlayerInfo { UserId = 43, Nickname = "member" }
            ]
        });

        var players = new PlayerManager();
        var manager = new RoomManager(players, new GameServerRegistry(), store);

        manager.RestoreFromStore();

        var room = manager.GetRoom("room-1");
        Assert.NotNull(room);
        Assert.Equal(new long[] { 42, 43 }, room!.PlayerIds);
        Assert.Equal(42L, room.OwnerUserId);
        Assert.Equal(4, room.MaxPlayers);
        Assert.Equal("127.0.0.1", room.GameServerAddress);

        var owner = players.Get(42);
        Assert.NotNull(owner);
        Assert.Null(owner!.Peer);
        Assert.Equal(PlayerState.InRoom, owner.State);
        Assert.Equal("room-1", owner.CurrentRoomId);
        Assert.False(players.IsOnline(42));
    }

    [Fact]
    public void Restore_DeletesEmptyPlayerRoom_KeepsEmptySystemRoom()
    {
        var store = new InMemoryLobbyRoomStore();
        store.Save(new LobbyRoomSnapshot
        {
            RoomId = "empty-player-room",
            CreatorUserId = 42,
            GameServerAddress = "127.0.0.1",
            GameServerPort = 7001
        });
        store.Save(new LobbyRoomSnapshot
        {
            RoomId = "empty-system-room",
            CreatorUserId = null,
            GameServerAddress = "127.0.0.1",
            GameServerPort = 7001
        });

        var manager = new RoomManager(new PlayerManager(), new GameServerRegistry(), store);
        manager.RestoreFromStore();

        Assert.Null(manager.GetRoom("empty-player-room"));
        Assert.NotNull(manager.GetRoom("empty-system-room"));
        Assert.DoesNotContain(store.LoadAll(), r => r.RoomId == "empty-player-room");
    }

    [Fact]
    public void Restore_OwnerNotInMembers_FallsBackToFirstMember()
    {
        var store = new InMemoryLobbyRoomStore();
        store.Save(new LobbyRoomSnapshot
        {
            RoomId = "room-owner-fallback",
            OwnerUserId = 999,
            CreatorUserId = 999,
            GameServerAddress = "127.0.0.1",
            GameServerPort = 7001,
            Players = [new PlayerInfo { UserId = 50, Nickname = "first" }]
        });

        var manager = new RoomManager(new PlayerManager(), new GameServerRegistry(), store);
        manager.RestoreFromStore();

        Assert.Equal(50L, manager.GetRoom("room-owner-fallback")!.OwnerUserId);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test GameServer.Tests --nologo --filter FullyQualifiedName~RoomManagerRestoreTests`
Expected: 编译失败，提示 `RoomManager` 构造函数不匹配 / `RestoreFromStore` / `GetRoom` 不存在。

- [ ] **Step 3: RoomManager 注入存储、恢复与持久化**

在 `LobbyServer/Room/RoomManager.cs` 顶部加入：

```csharp
using LobbyServer.Persistence;
```

字段与构造函数：

```csharp
private readonly PlayerManager _players;
private readonly GameServerRegistry _gameServerRegistry;
private readonly ILobbyRoomStore _roomStore;
private readonly Dictionary<string, LobbyRoom> _rooms = new();

public RoomManager(PlayerManager players, GameServerRegistry gameServerRegistry, ILobbyRoomStore roomStore)
{
    _players = players;
    _gameServerRegistry = gameServerRegistry;
    _roomStore = roomStore;
}
```

在 `CreateRoom` 中，`_rooms[roomId] = room;` 之后、日志之前加入：

```csharp
PersistRoom(room);
```

在 `JoinRoom` 的 `return` 之前加入：

```csharp
PersistRoom(room);
```

在 `LeaveRoom` 中：

- `remaining == 0` 分支加入：

```csharp
_roomStore.Delete(roomId);
```

- `else` 分支末尾（`LeaveRoomNotify` 之后）加入：

```csharp
PersistRoom(room);
```

在 `CloseRoom` 的 `_rooms.Remove(roomId, out var room)` 成功后加入：

```csharp
_roomStore.Delete(roomId);
```

新增方法：

```csharp
[MethodImpl(MethodImplOptions.Synchronized)]
public LobbyRoom? GetRoom(string roomId)
{
    return _rooms.TryGetValue(roomId, out var room) ? room : null;
}

[MethodImpl(MethodImplOptions.Synchronized)]
public void PersistPlayerRoom(long userId)
{
    var player = _players.Get(userId);
    if (player?.CurrentRoomId is not { } roomId)
        return;

    if (_rooms.TryGetValue(roomId, out var room))
        PersistRoom(room);
}

[MethodImpl(MethodImplOptions.Synchronized)]
public void RestoreFromStore()
{
    var snapshots = _roomStore.LoadAll();

    foreach (var snapshot in snapshots)
    {
        var snapshotPlayers = snapshot.Players ?? [];

        if (snapshotPlayers.Count == 0 && snapshot.CreatorUserId != null)
        {
            Log.Warning("[RoomManager] 恢复时发现空玩家房，删除 roomId={RoomId}", snapshot.RoomId);
            _roomStore.Delete(snapshot.RoomId);
            continue;
        }

        var room = new LobbyRoom
        {
            RoomId = snapshot.RoomId,
            RoomType = snapshot.RoomType,
            MaxPlayers = snapshot.MaxPlayers,
            OwnerUserId = snapshot.OwnerUserId,
            CreatorUserId = snapshot.CreatorUserId,
            GameServerAddress = snapshot.GameServerAddress,
            GameServerPort = snapshot.GameServerPort
        };

        foreach (var info in snapshotPlayers)
        {
            if (_players.Get(info.UserId) != null)
            {
                Log.Warning("[RoomManager] 恢复时发现重复玩家 userId={UserId} roomId={RoomId}",
                    info.UserId, snapshot.RoomId);
                continue;
            }

            room.PlayerIds.Add(info.UserId);
            _players.AddOffline(info, snapshot.RoomId);
        }

        if (room.PlayerIds.Count == 0 && room.CreatorUserId != null)
        {
            _roomStore.Delete(snapshot.RoomId);
            continue;
        }

        if (room.PlayerIds.Count > 0 && !room.PlayerIds.Contains(room.OwnerUserId))
        {
            Log.Warning("[RoomManager] 房主不在成员列表，回退到第一个成员 roomId={RoomId}", snapshot.RoomId);
            room.OwnerUserId = room.PlayerIds[0];
        }

        _rooms[room.RoomId] = room;
    }

    Log.Information("[RoomManager] 从存储恢复房间数={Count}", _rooms.Count);
}
```

新增私有方法：

```csharp
private void PersistRoom(LobbyRoom room)
{
    var players = new List<PlayerInfo>();

    foreach (var userId in room.PlayerIds)
    {
        var player = _players.Get(userId);
        if (player == null)
        {
            Log.Warning("[RoomManager] 持久化时玩家不存在 userId={UserId} roomId={RoomId}",
                userId, room.RoomId);
            continue;
        }

        players.Add(player.Info);
    }

    _roomStore.Save(new LobbyRoomSnapshot
    {
        RoomId = room.RoomId,
        RoomType = room.RoomType,
        MaxPlayers = room.MaxPlayers,
        OwnerUserId = room.OwnerUserId,
        CreatorUserId = room.CreatorUserId,
        GameServerAddress = room.GameServerAddress,
        GameServerPort = room.GameServerPort,
        Players = players
    });
}
```

- [ ] **Step 4: LobbyManager 重连后保存快照**

在 `LobbyManager.Join` 的 `_players.BindOnline(existing, peer, request.Player);` 之后加入：

```csharp
_rooms.PersistPlayerRoom(userId);
```

- [ ] **Step 5: LobbyServer 注入存储并恢复**

Modify `LobbyServer`：

```csharp
using LobbyServer.Persistence;

private readonly ILobbyRoomStore _roomStore;
private readonly bool _ownsRoomStore;
```

构造函数签名与初始化：

```csharp
public LobbyServer(LobbyServerConfig config, ILobbyRoomStore? roomStore = null)
{
    _ownsRoomStore = roomStore == null;
    _roomStore = roomStore ?? new RedisLobbyRoomStore(
        config.RedisConnection, config.RedisKeyPrefix, config.RedisDatabase);

    _clientKey = config.ClientConnectionKey;
    _serverKey = config.ServerConnectionKey;

    _clientListener = new EventBasedNetListener();
    _clientNetManager = new NetManager(_clientListener)
    {
        UpdateTime = config.UpdateTime,
        PingInterval = config.PingInterval,
        DisconnectTimeout = config.DisconnectTimeout,
        ChannelsCount = config.ChannelsCount
    };

    _serverListener = new EventBasedNetListener();
    _serverNetManager = new NetManager(_serverListener)
    {
        UpdateTime = config.UpdateTime,
        PingInterval = config.PingInterval,
        DisconnectTimeout = config.DisconnectTimeout,
        ChannelsCount = config.ChannelsCount
    };
}
```

`Start` 中：

```csharp
_roomManager = new RoomManager(_players, _gameServerRegistry, _roomStore);
_roomManager.RestoreFromStore();
_lobbyManager = new LobbyManager(_clientNetManager, _players, _roomManager);
```

- [ ] **Step 6: 测试 fixture 注入内存存储**

Modify `GameServer.Tests/ServerFixture.cs`：

```csharp
using LobbyServer.Persistence;
```

字段：

```csharp
private readonly ILobbyRoomStore _roomStore;
private readonly LobbyServerConfig _lobbyConfig;
private readonly GameServerConfig _gameConfig;
```

构造函数改为：

```csharp
public ServerFixture(ILobbyRoomStore? roomStore = null)
{
    Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

    _roomStore = roomStore ?? new InMemoryLobbyRoomStore();

    var ports = TestPorts.NextFree(3);
    LobbyClientPort = ports[0];
    LobbyServerPort = ports[1];
    GameServerPort = ports[2];

    _lobbyConfig = new LobbyServerConfig
    {
        ClientPort = LobbyClientPort,
        ServerPort = LobbyServerPort,
        ClientConnectionKey = ClientKey,
        ServerConnectionKey = ServerKey
    };

    _gameConfig = new GameServerConfig
    {
        Port = GameServerPort,
        LobbyAddress = "127.0.0.1",
        LobbyPort = LobbyServerPort,
        LobbyConnectionKey = ServerKey,
        ClientConnectionKey = ClientKey
    };

    _lobby = new LobbyServer.LobbyServer(_lobbyConfig, _roomStore);
    _lobby.Start(_lobbyConfig);

    _game = new global::GameServer.GameServer(_gameConfig);
    _game.Start();

    _pollTask = Task.Run(async () =>
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(5));
        while (!_cts.Token.IsCancellationRequested)
        {
            _lobby.PollEvents();
            _game.PollEvents();
            try
            {
                await timer.WaitForNextTickAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    });

    Thread.Sleep(1000);
}
```

- [ ] **Step 7: 运行恢复测试确认通过**

Run: `dotnet test GameServer.Tests --nologo --filter FullyQualifiedName~RoomManagerRestoreTests`
Expected: PASS（3 个测试）。

- [ ] **Step 8: 跑全量测试并提交**

Run: `dotnet test GameServer.Tests --nologo`
Expected: 全部 PASS。

Run: `dotnet build GameServer.sln`
Expected: 0 警告 0 错误。

```bash
git add LobbyServer/Room/RoomManager.cs LobbyServer/Lobby/LobbyManager.cs \
  LobbyServer/LobbyServer.cs GameServer.Tests/ServerFixture.cs \
  GameServer.Tests/RoomManagerRestoreTests.cs
git commit -m "接入房间持久化与启动恢复"
```

---

## Task 5: GameServer 注册补建、Lobby 停机与重启测试

**Files:**
- Modify: `LobbyServer/Room/RoomManager.cs`
- Modify: `LobbyServer/Handlers/GameServerRegisterHandler.cs`
- Modify: `LobbyServer/LobbyServer.cs`
- Modify: `LobbyServer/Program.cs`
- Modify: `GameServer.Tests/ServerFixture.cs`
- Test: `GameServer.Tests/GameServerRestartTests.cs`
- Test: `GameServer.Tests/LobbyRestartTests.cs`
- Modify: `AGENTS.md`

**Interfaces:**
- Consumes: Task 4 的 `RoomManager` 存储/恢复、`LobbyServer` 构造
- Produces:
  - `RoomManager.ResyncGameServer(NetPeer peer, GameServerInfo info) : void`
  - `LobbyServer.Stop() : void`
  - `ServerFixture.RestartLobby()` / `RestartGameServer()`

- [ ] **Step 1: 写失败的重启测试**

Create `GameServer.Tests/GameServerRestartTests.cs`:

```csharp
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;

namespace GameServer.Tests;

public class GameServerRestartTests
{
    private readonly ServerFixture _fixture = new();

    private TestClient ConnectLobbyClient(long userId, string nickname)
    {
        var client = new TestClient();
        client.Connect(_fixture.LobbyClientPort, ServerFixture.ClientKey);
        Assert.True(client.WaitConnected());
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
        Assert.True(client.WaitConnected());
        return client;
    }

    private static void CreateRoom(TestClient client, string roomId)
    {
        client.Send(MessageIds.CreateRoom, new CreateRoomRequest
        {
            RoomId = roomId,
            RoomType = RoomType.Default,
            MaxPlayers = 4
        });
        Assert.NotNull(client.WaitPayload<CreateRoomResponse>(MessageIds.CreateRoom));
    }

    private static (ushort MessageId, ReturnCode Code, byte[] Payload) JoinGameUntil(
        TestClient client, string roomId, long userId, Func<ReturnCode, bool> condition, int timeoutMs = 5000)
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
    public void GameServerRestart_RecreatesGameRooms()
    {
        using var lobby = ConnectLobbyClient(5101, "owner");
        CreateRoom(lobby, "restart-gs-room");

        using var gameBefore = ConnectGameClient();
        JoinGameUntil(gameBefore, "restart-gs-room", 5101, c => c == ReturnCode.Success);
        gameBefore.Dispose();

        _fixture.RestartGameServer();

        using var gameAfter = ConnectGameClient();
        var message = JoinGameUntil(gameAfter, "restart-gs-room", 5101, c => c == ReturnCode.Success);

        Assert.Equal(ReturnCode.Success, message.Code);
    }
}
```

Create `GameServer.Tests/LobbyRestartTests.cs`:

```csharp
using LobbyServer.Persistence;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Redis;

namespace GameServer.Tests;

public class LobbyRestartTests : IDisposable
{
    private readonly string _prefix = $"test:lobbystart:{Guid.NewGuid():N}";
    private readonly RedisHelper _redis = new("localhost:6379");
    private readonly RedisLobbyRoomStore _store;
    private readonly ServerFixture _fixture;

    public LobbyRestartTests()
    {
        _store = new RedisLobbyRoomStore("localhost:6379", _prefix);
        _fixture = new ServerFixture(_store);
    }

    private TestClient ConnectClient(long userId, string nickname)
    {
        var client = new TestClient();
        client.Connect(_fixture.LobbyClientPort, ServerFixture.ClientKey);
        Assert.True(client.WaitConnected());
        client.Send(MessageIds.JoinLobby, new JoinLobbyRequest
        {
            Player = new PlayerInfo { UserId = userId, Nickname = nickname }
        });
        Assert.NotNull(client.WaitPayload<JoinLobbyResponse>(MessageIds.JoinLobby));
        return client;
    }

    private static CreateRoomResponse CreateRoom(TestClient client, string roomId)
    {
        client.Send(MessageIds.CreateRoom, new CreateRoomRequest
        {
            RoomId = roomId,
            RoomType = RoomType.Default,
            MaxPlayers = 4
        });
        var response = client.WaitPayload<CreateRoomResponse>(MessageIds.CreateRoom);
        Assert.NotNull(response);
        return response!;
    }

    private static JoinRoomResponse JoinRoom(TestClient client, string roomId)
    {
        client.Send(MessageIds.JoinRoom, new JoinRoomRequest { RoomId = roomId });
        var response = client.WaitPayload<JoinRoomResponse>(MessageIds.JoinRoom);
        Assert.NotNull(response);
        return response!;
    }

    [Fact]
    public void LobbyRestart_RestoresRoomsAndMembers()
    {
        var owner = ConnectClient(5201, "owner");
        CreateRoom(owner, "restart-lobby-room");
        var member = ConnectClient(5202, "member");
        JoinRoom(member, "restart-lobby-room");

        _fixture.RestartLobby();

        owner.Dispose();
        member.Dispose();

        using var ownerAgain = ConnectClient(5201, "owner");
        using var memberAgain = ConnectClient(5202, "member");
        using var checker = ConnectClient(5203, "checker");

        var joined = JoinRoom(checker, "restart-lobby-room");

        Assert.Equal(3, joined.Room.Players.Count);
        Assert.Equal(5201L, joined.Room.OwnerUserId);
        Assert.Contains(joined.Room.Players, p => p.UserId == 5201);
        Assert.Contains(joined.Room.Players, p => p.UserId == 5202);
    }

    public void Dispose()
    {
        _fixture.Dispose();
        _store.Dispose();
        foreach (var key in _redis.ScanKeys($"{_prefix}:*"))
            _redis.Delete(key);
        _redis.Dispose();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test GameServer.Tests --nologo --filter "FullyQualifiedName~GameServerRestartTests|FullyQualifiedName~LobbyRestartTests"`
Expected: 编译失败（`RestartGameServer` / `RestartLobby` / `LobbyServer.Stop` 不存在）。

- [ ] **Step 3: RoomManager 增加 GS 补建**

在 `RoomManager` 中加入：

```csharp
[MethodImpl(MethodImplOptions.Synchronized)]
public void ResyncGameServer(NetPeer peer, GameServerInfo info)
{
    var rooms = _rooms.Values
        .Where(r => r.GameServerAddress == info.Address && r.GameServerPort == info.Port)
        .ToList();

    foreach (var room in rooms)
    {
        var frame = MessageHelper.CreateFrame(MessageIds.CreateGameRoom, ReturnCode.Success,
            new CreateGameRoomRequest
            {
                RoomId = room.RoomId,
                RoomType = room.RoomType,
                MaxPlayers = room.MaxPlayers
            });

        if (!MessageHelper.TrySend(peer, frame))
            Log.Warning("[RoomManager] 补建游戏房间失败 roomId={RoomId}", room.RoomId);
    }

    Log.Information("[RoomManager] GameServer注册补建完成 GameServer={Addr}:{Port} 房间数={Count}",
        info.Address, info.Port, rooms.Count);
}
```

- [ ] **Step 4: 注册 handler 触发补建**

Replace `LobbyServer/Handlers/GameServerRegisterHandler.cs`:

```csharp
using LiteNetLib;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using LobbyServer.Cluster;
using LobbyServer.Room;

namespace LobbyServer.Handlers;

public class GameServerRegisterHandler : MessageHandler<GameServerInfo>
{
    public override ushort MessageId => MessageIds.GameServerRegister;

    private readonly GameServerRegistry _registry;
    private readonly RoomManager _rooms;

    public GameServerRegisterHandler(GameServerRegistry registry, RoomManager rooms)
    {
        _registry = registry;
        _rooms = rooms;
    }

    public override void HandleMessage(NetPeer peer, GameServerInfo request)
    {
        _registry.Register(peer, request);
        Log.Information("[LobbyServer] GameServer 注册成功 address={Address} port={Port}", request.Address, request.Port);

        _rooms.ResyncGameServer(peer, request);
    }
}
```

Modify `LobbyServer.RegisterHandlers`：

```csharp
new GameServerRegisterHandler(_gameServerRegistry, _roomManager),
```

- [ ] **Step 5: LobbyServer 增加 Stop（停机时不删除房间）**

在 `LobbyServer` 字段区加入：

```csharp
private bool _stopping;
```

加入 `Stop`：

```csharp
public void Stop()
{
    if (_stopping)
        return;

    _stopping = true;

    _clientNetManager.Stop();
    _serverNetManager.Stop();

    if (_ownsRoomStore)
        _roomStore.Dispose();
}
```

修改 `OnClientDisconnected`，停机期间不要删除房间/玩家（否则重启恢复会被清空）：

```csharp
private void OnClientDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
{
    if (_stopping)
        return;

    Log.Information("[LobbyServer] 客户端断开 endpoint={EndPoint} reason={Reason}",
        peer.Address, disconnectInfo.Reason);

    _roomManager.RemovePlayer(peer);
}
```

同时修改 `OnServerDisconnected`，停机期间跳过 registry 清理：

```csharp
private void OnServerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
{
    if (_stopping)
        return;

    Log.Information("[LobbyServer] GameServer断开 endpoint={EndPoint} reason={Reason}",
        peer.Address, disconnectInfo.Reason);

    _gameServerRegistry.Remove(peer);
}
```

- [ ] **Step 6: ServerFixture 支持重启**

Modify `GameServer.Tests/ServerFixture.cs`，在类中加入：

```csharp
public void RestartLobby()
{
    lock (_pollGate)
    {
        _lobby.Stop();
        Thread.Sleep(200);

        _lobby = new LobbyServer.LobbyServer(_lobbyConfig, _roomStore);
        _lobby.Start(_lobbyConfig);
    }

    Thread.Sleep(500);
}

public void RestartGameServer()
{
    lock (_pollGate)
    {
        _game.Stop();
        Thread.Sleep(200);

        _game = new global::GameServer.GameServer(_gameConfig);
        _game.Start();
    }

    Thread.Sleep(1000);
}
```

在字段区加入：

```csharp
private readonly object _pollGate = new();
```

把 poll task 的循环体改为：

```csharp
lock (_pollGate)
{
    _lobby.PollEvents();
    _game.PollEvents();
}
```

把 `StopGameServer` 改为：

```csharp
public void StopGameServer()
{
    lock (_pollGate)
    {
        _game.Stop();
    }

    Thread.Sleep(300);
}
```

把 `Dispose` 改为：

```csharp
public void Dispose()
{
    _cts.Cancel();

    try
    {
        _pollTask.Wait(2000);
    }
    catch (AggregateException)
    {
    }

    _game.Stop();
    _lobby.Stop();
}
```

- [ ] **Step 7: Program 接入停机与 Redis 启动失败**

Modify `LobbyServer/Program.cs`：

```csharp
LobbyServer.LobbyServer server;

try
{
    server = new LobbyServer.LobbyServer(config);
}
catch (Exception ex)
{
    Log.Fatal(ex, "[LobbyServer] 初始化失败，请检查 Redis 连接");
    Environment.ExitCode = 1;
    return;
}

server.Start(config);
```

把主循环包在 `try/finally` 中：

```csharp
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
finally
{
    server.Stop();
}
```

- [ ] **Step 8: 更新 AGENTS.md**

在「构建与运行」中加入：

```markdown
Redis 必须先启动（默认 `localhost:6379`），否则 LobbyServer 启动失败。启动顺序：Redis → LobbyServer → GameServer → TestClient。
```

在「约定」中加入：

```markdown
- LobbyServer 的房间、成员、房主持久化到 Redis（`{RedisKeyPrefix}:lobby:rooms` Hash）；GameServer 不持久化房间。
- Lobby 重启从 Redis 恢复房间，成员恢复为离线状态，玩家重新 `JoinLobby`（相同 userId）后回到原房间。
- GameServer 重启后重新注册时，Lobby 按 `Address:Port` 补发 `CreateGameRoom`（幂等）；游戏内玩家需要重新 `JoinGame`。
- Redis 配置：`RedisConnection`、`RedisKeyPrefix`、`RedisDatabase`（见 `LobbyServerConfig.json`）。
- Redis 启动失败：Lobby 退出；运行期写失败：记录日志，内存状态继续。
```

- [ ] **Step 9: 运行重启测试确认通过**

Run: `dotnet test GameServer.Tests --nologo --filter "FullyQualifiedName~GameServerRestartTests|FullyQualifiedName~LobbyRestartTests"`
Expected: PASS。

- [ ] **Step 10: 跑全量测试并提交**

Run: `dotnet test GameServer.Tests --nologo`
Expected: 全部 PASS。

Run: `dotnet build GameServer.sln`
Expected: 0 警告 0 错误。

```bash
git add LobbyServer/Room/RoomManager.cs LobbyServer/Handlers/GameServerRegisterHandler.cs \
  LobbyServer/LobbyServer.cs LobbyServer/Program.cs GameServer.Tests/ServerFixture.cs \
  GameServer.Tests/GameServerRestartTests.cs GameServer.Tests/LobbyRestartTests.cs AGENTS.md
git commit -m "支持GameServer重启补建与Lobby停机恢复"
```

---

## Task 6: 手动冒烟（建议）

**Files:** 无新增

- [ ] **Step 1: 启动顺序**

```bash
redis-cli ping          # 期望 PONG
dotnet run --project LobbyServer
dotnet run --project GameServer
dotnet run --project TestClient
```

- [ ] **Step 2: 建一个房间并确认 Redis 有数据**

在 TestClient：

```text
joinlobby
createroom 4 redis-smoke
```

在终端：

```bash
redis-cli HGETALL gameserver:lobby:rooms
```

Expected: 能看到 `redis-smoke` 对应的 JSON，包含房主和成员。

- [ ] **Step 3: 重启 LobbyServer**

停止 LobbyServer，再 `dotnet run --project LobbyServer`。重新启动 TestClient：

```text
joinlobby
joinroom redis-smoke
```

Expected: 房间仍在，原成员 userId 仍在成员列表，房主仍是原 owner（新 TestClient 是另一个 userId，加入后可见原成员）。

- [ ] **Step 4: 重启 GameServer**

停止 GameServer，再启动；在 TestClient 执行：

```text
joingame redis-smoke
```

Expected: 能进入游戏房间（Lobby 已补建）。

- [ ] **Step 5: 清理**

停止三个进程；如需清理测试数据：

```bash
redis-cli HDEL gameserver:lobby:rooms redis-smoke
```
