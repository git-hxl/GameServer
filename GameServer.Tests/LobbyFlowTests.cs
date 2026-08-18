using SharedLib.Models;
using SharedLib.Protocol;

namespace GameServer.Tests;

public class LobbyFlowTests
{
    private readonly ServerFixture _fixture;

    public LobbyFlowTests()
    {
        _fixture = new ServerFixture();
    }

    private TestClient ConnectClient(int port, long userId, string nickname)
    {
        var client = new TestClient();
        client.Connect(port, ServerFixture.ClientKey);
        Assert.True(client.WaitConnected(), "客户端未能在超时内连接");
        client.Send(MessageIds.JoinLobby, new JoinLobbyRequest { Player = new PlayerInfo { UserId = userId, Nickname = nickname } });
        var res = client.WaitPayload<JoinLobbyResponse>(MessageIds.JoinLobby);
        Assert.NotNull(res);
        return client;
    }

    [Fact]
    public void JoinLobby_ShouldSucceed()
    {
        using var client = new TestClient();
        client.Connect(_fixture.LobbyClientPort, ServerFixture.ClientKey);
        Assert.True(client.WaitConnected());

        client.Send(MessageIds.JoinLobby, new JoinLobbyRequest { Player = new PlayerInfo { UserId = 1001, Nickname = "tester" } });

        var res = client.WaitPayload<JoinLobbyResponse>(MessageIds.JoinLobby);
        Assert.NotNull(res);
        Assert.Equal(1001, res!.Player.UserId);
    }

    [Fact]
    public void CreateRoom_And_Join_Flow()
    {
        using var owner = ConnectClient(_fixture.LobbyClientPort, 2001, "owner");

        owner.Send(MessageIds.CreateRoom, new CreateRoomRequest { RoomId = "itroom1", RoomType = RoomType.Default });
        var createRes = owner.WaitPayload<CreateRoomResponse>(MessageIds.CreateRoom);
        Assert.NotNull(createRes);
        Assert.Equal("itroom1", createRes!.Room.RoomId);

        using var joiner = ConnectClient(_fixture.LobbyClientPort, 2002, "joiner");

        joiner.Send(MessageIds.JoinRoom, new JoinRoomRequest { RoomId = "itroom1" });
        var joinRes = joiner.WaitPayload<JoinRoomResponse>(MessageIds.JoinRoom);
        Assert.NotNull(joinRes);
        Assert.Equal(2, joinRes!.Room.Players.Count);
    }

    [Fact]
    public void JoinRoom_ToStartedRoom_ShouldFail_RoomFull()
    {
        using var owner = ConnectClient(_fixture.LobbyClientPort, 3001, "owner");

        owner.Send(MessageIds.CreateRoom, new CreateRoomRequest { RoomId = "itroom2", RoomType = RoomType.Default, MaxPlayers = 1 });
        Assert.NotNull(owner.WaitPayload<CreateRoomResponse>(MessageIds.CreateRoom));

        owner.Send(MessageIds.GameReady, new GameReadyRequest());
        Assert.NotNull(owner.WaitPayload<GameReadyResponse>(MessageIds.GameReady));

        owner.Send(MessageIds.GameStart, new GameStartRequest());
        var startMsg = owner.WaitMessage(MessageIds.GameStart);
        Assert.NotNull(startMsg);
        Assert.Equal(ReturnCode.Success, startMsg!.Value.Code);
        Assert.NotNull(owner.WaitPayload<GameStartNotify>(MessageIds.GameStartNotify));

        using var joiner = ConnectClient(_fixture.LobbyClientPort, 3002, "joiner");

        joiner.Send(MessageIds.JoinRoom, new JoinRoomRequest { RoomId = "itroom2" });
        var joinRes = joiner.WaitMessage(MessageIds.JoinRoom);
        Assert.NotNull(joinRes);
        Assert.Equal(ReturnCode.RoomFull, joinRes!.Value.Code);
    }

    [Fact]
    public void QuickMatch_Room_Unrelated_Player_JoinGame_ShouldBeRejected()
    {
        using var owner = ConnectClient(_fixture.LobbyClientPort, 4001, "owner");

        owner.Send(MessageIds.CreateRoom, new CreateRoomRequest { RoomId = "itroom3", RoomType = RoomType.QuickMatch, MaxPlayers = 4 });
        Assert.NotNull(owner.WaitPayload<CreateRoomResponse>(MessageIds.CreateRoom));

        owner.Send(MessageIds.GameStart, new GameStartRequest());
        var notify = owner.WaitPayload<GameStartNotify>(MessageIds.GameStartNotify);
        Assert.NotNull(notify);

        using var stranger = new TestClient();
        stranger.Connect(_fixture.GameServerPort, ServerFixture.ClientKey);
        Assert.True(stranger.WaitConnected());
        stranger.Send(MessageIds.JoinGame, new JoinGameRequest
        {
            RoomId = "itroom3",
            Player = new PlayerInfo { UserId = 9999, Nickname = "stranger" }
        });
        var joinGame = stranger.WaitMessage(MessageIds.JoinGame);
        Assert.NotNull(joinGame);
        Assert.Equal(ReturnCode.NotAuthorized, joinGame!.Value.Code);
    }

    [Fact]
    public void StartGame_AfterGameServerGoesOffline_ShouldFail()
    {
        using var owner = ConnectClient(_fixture.LobbyClientPort, 5001, "owner");

        owner.Send(MessageIds.CreateRoom, new CreateRoomRequest { RoomId = "itroom4", RoomType = RoomType.Default });
        Assert.NotNull(owner.WaitPayload<CreateRoomResponse>(MessageIds.CreateRoom));

        _fixture.StopGameServer();

        owner.Send(MessageIds.GameReady, new GameReadyRequest());
        Assert.NotNull(owner.WaitPayload<GameReadyResponse>(MessageIds.GameReady));

        owner.Send(MessageIds.GameStart, new GameStartRequest());
        var start = owner.WaitMessage(MessageIds.GameStart);
        Assert.NotNull(start);
        Assert.Equal(ReturnCode.GameServerOffline, start!.Value.Code);
    }
}
