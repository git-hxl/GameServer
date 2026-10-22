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
            RoomType = RoomType.Default,
            MaxPlayers = 8
        });
        Assert.Equal("r1", create.RoomId);
        Assert.Equal(RoomType.Default, create.RoomType);
        Assert.Equal(8, create.MaxPlayers);
    }

    private static T RoundTrip<T>(T value) where T : class
    {
        var frame = MessageHelper.CreateFrame(999, ReturnCode.Success, value);
        return MessageHelper.Deserialize<T>(frame)!;
    }
}
