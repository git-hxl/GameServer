using SharedLib.Models;

namespace GameServer.Room;

public class GameRoom
{
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public long OwnerUserId { get; set; }
    public HashSet<long> PlayerIds { get; set; } = [];
    public HashSet<long> AllowedPlayerIds { get; set; } = [];
}
