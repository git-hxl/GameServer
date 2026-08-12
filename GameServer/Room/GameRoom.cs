using LiteNetLib;
using SharedLib.Models;

namespace GameServer.Room;

public class GameRoom
{
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public long OwnerUserId { get; set; }
    public Dictionary<NetPeer, PlayerInfo> Players { get; set; } = [];
    public bool IsStarted { get; set; }
}
