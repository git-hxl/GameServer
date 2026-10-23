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
