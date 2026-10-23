using LobbyServer.Persistence;
using SharedLib.Models;

namespace GameServer.Tests;

public sealed class InMemoryLobbyRoomStore : ILobbyRoomStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, LobbyRoomSnapshot> _rooms = new();

    public IReadOnlyList<LobbyRoomSnapshot> LoadAll()
    {
        lock (_gate)
            return _rooms.Values.Select(Clone).ToList();
    }

    public void Save(LobbyRoomSnapshot room)
    {
        lock (_gate)
            _rooms[room.RoomId] = Clone(room);
    }

    public void Delete(string roomId)
    {
        lock (_gate)
            _rooms.Remove(roomId);
    }

    public void Dispose()
    {
    }

    private static LobbyRoomSnapshot Clone(LobbyRoomSnapshot room)
    {
        return new LobbyRoomSnapshot
        {
            Version = room.Version,
            RoomId = room.RoomId,
            RoomType = room.RoomType,
            MaxPlayers = room.MaxPlayers,
            OwnerUserId = room.OwnerUserId,
            CreatorUserId = room.CreatorUserId,
            GameServerAddress = room.GameServerAddress,
            GameServerPort = room.GameServerPort,
            Players = room.Players.Select(p => new PlayerInfo
            {
                UserId = p.UserId,
                Nickname = p.Nickname,
                Gender = p.Gender,
                Avatar = p.Avatar,
                Age = p.Age,
                ExtraData = new Dictionary<string, string>(p.ExtraData ?? new Dictionary<string, string>())
            }).ToList()
        };
    }
}
