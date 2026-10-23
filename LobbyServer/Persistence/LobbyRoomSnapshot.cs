using SharedLib.Models;

namespace LobbyServer.Persistence;

public class LobbyRoomSnapshot
{
    public int Version { get; set; } = 1;
    public string RoomId { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int MaxPlayers { get; set; }
    public long OwnerUserId { get; set; }
    public long? CreatorUserId { get; set; }
    public string GameServerAddress { get; set; } = string.Empty;
    public int GameServerPort { get; set; }
    public List<PlayerInfo> Players { get; set; } = [];
}
