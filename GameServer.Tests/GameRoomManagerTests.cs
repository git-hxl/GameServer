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
