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
