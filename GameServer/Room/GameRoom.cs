using System.Collections.Concurrent;
using SharedLib.Models;
using GameServer.Player;

namespace GameServer.Room;

public class GameRoom
{
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public long OwnerUserId { get; set; }
    public ConcurrentDictionary<long, GamePlayer> Players { get; set; } = new();
    public bool IsStarted { get; set; }
}
