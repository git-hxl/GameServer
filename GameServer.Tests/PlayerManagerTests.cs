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
