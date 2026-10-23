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
    public void JoinRoom_WhenAlreadyMember_ReturnsSuccess()
    {
        using var owner = ConnectClient(2051, "owner");
        CreateRoom(owner, "room-rejoin-self");

        var joined = JoinRoom(owner, "room-rejoin-self");

        Assert.Equal("room-rejoin-self", joined.Room.RoomId);
        Assert.Single(joined.Room.Players);
    }

    [Fact]
    public void JoinLobby_TwoUserIdsOnSamePeer_RemovesFirst()
    {
        using var client = ConnectClient(2061, "first");
        CreateRoom(client, "room-multi-id");

        client.Send(MessageIds.JoinLobby, new JoinLobbyRequest
        {
            Player = new PlayerInfo { UserId = 2062, Nickname = "second" }
        });
        Assert.NotNull(client.WaitMessage(MessageIds.JoinLobby));

        using var checker = ConnectClient(2063, "checker");
        JoinRoom(checker, "room-multi-id", ReturnCode.RoomNotFound);
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

    [Fact]
    public void CreateSystemRoom_ConcurrentCalls_AllSucceed()
    {
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var codes = new System.Collections.Concurrent.ConcurrentBag<ReturnCode>();

        Parallel.For(0, 20, i =>
        {
            try
            {
                codes.Add(_fixture.CreateSystemRoom($"room-concurrent-{i}").Code);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.All(codes, code => Assert.Equal(ReturnCode.Success, code));
    }
}
