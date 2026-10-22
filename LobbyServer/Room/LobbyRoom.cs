using SharedLib.Models;

namespace LobbyServer.Room;

public class LobbyRoom
{
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int MaxPlayers { get; set; }
    public List<long> PlayerIds { get; set; } = [];
    public long OwnerUserId { get; set; }
    public long? CreatorUserId { get; set; }
    public string GameServerAddress { get; set; } = string.Empty;
    public int GameServerPort { get; set; }
}
