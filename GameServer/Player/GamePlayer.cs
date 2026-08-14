using LiteNetLib;
using SharedLib.Models;

namespace GameServer.Player;

public class GamePlayer
{
    public PlayerInfo Info { get; set; } = new();
    public NetPeer Peer { get; set; } = null!;
    public string? CurrentRoomId { get; set; }
}
