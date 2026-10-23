using SharedLib.Models;
using SharedLib.Protocol;

namespace GameServer.Tests;

public class DisconnectBehaviorTests
{
    private readonly ServerFixture _fixture = new(disconnectTimeoutMs: 1500);

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

    private static void PollFor(TestClient client, int milliseconds)
    {
        var deadline = Environment.TickCount64 + milliseconds;
        while (Environment.TickCount64 < deadline)
        {
            client.Poll();
            Thread.Sleep(10);
        }
    }

    [Fact]
    public void ActiveDisconnect_RemovesPlayerFromRoom()
    {
        var owner = ConnectClient(2101, "owner");
        CreateRoom(owner, "room-active-disconnect");

        using var member = ConnectClient(2102, "member");
        JoinRoom(member, "room-active-disconnect");

        owner.Dispose();
        PollFor(member, 400);

        using var checker = ConnectClient(2103, "checker");
        var joined = JoinRoom(checker, "room-active-disconnect");

        Assert.Equal(2, joined.Room.Players.Count);
        Assert.DoesNotContain(joined.Room.Players, p => p.UserId == 2101);
        Assert.Equal(2102L, joined.Room.OwnerUserId);
    }

    [Fact]
    public void AbnormalDisconnect_KeepsPlayerForReconnect()
    {
        var owner = ConnectClient(2201, "owner");
        CreateRoom(owner, "room-abnormal-disconnect");

        using var member = ConnectClient(2202, "member");
        JoinRoom(member, "room-abnormal-disconnect");

        PollFor(member, 2200);

        using var checker = ConnectClient(2203, "checker");
        var joined = JoinRoom(checker, "room-abnormal-disconnect");

        Assert.Equal(3, joined.Room.Players.Count);
        Assert.Equal(2201L, joined.Room.OwnerUserId);
        Assert.Contains(joined.Room.Players, p => p.UserId == 2201);

        using var ownerAgain = ConnectClient(2201, "owner-again");
        var rejoined = JoinRoom(ownerAgain, "room-abnormal-disconnect");
        Assert.Equal(3, rejoined.Room.Players.Count);

        ownerAgain.Send(MessageIds.LeaveRoom, new LeaveRoomRequest());
        var leave = ownerAgain.WaitMessage(MessageIds.LeaveRoom);
        Assert.NotNull(leave);
        Assert.Equal(ReturnCode.Success, leave!.Value.Code);

        owner.Dispose();
    }
}
