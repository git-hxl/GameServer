using System.Collections.Concurrent;
using LiteNetLib;
using SharedLib.Models;

namespace GameServer.Room;

public class GameRoom
{
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public long OwnerUserId { get; set; }
    public ConcurrentDictionary<NetPeer, PlayerInfo> Players { get; set; } = new();
    public bool IsStarted { get; set; }
}
