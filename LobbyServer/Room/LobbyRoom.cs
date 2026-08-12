using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Room;

public class LobbyRoom
{
    public RoomInfo Info { get; set; } = new();
    public NetPeer GameServerPeer { get; set; } = null!;
    public HashSet<long> PlayerIds { get; set; } = [];
    public HashSet<long> ReadyPlayerIds { get; set; } = [];
}
