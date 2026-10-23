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
