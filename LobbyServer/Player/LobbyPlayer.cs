using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Player;

public class LobbyPlayer
{
    public PlayerInfo Info { get; set; } = new();
    public NetPeer? Peer { get; set; }
    public string? CurrentRoomId { get; set; }
}
