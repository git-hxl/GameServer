using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Room;

public class LobbyRoom
{
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public long OwnerUserId { get; set; }
    public NetPeer GameServerPeer { get; set; } = null!;
    public HashSet<long> PlayerIds { get; set; } = [];
    public HashSet<long> ReadyPlayerIds { get; set; } = [];
    public int MaxPlayers { get; set; }
    public bool IsStarted { get; set; }
    public bool GameCreated { get; set; }
}
