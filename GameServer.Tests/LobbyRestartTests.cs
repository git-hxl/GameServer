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
