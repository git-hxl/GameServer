using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class RoomInfo
    {
        [Key(0)] public string RoomId { get; set; } = string.Empty;
        [Key(1)] public RoomType RoomType { get; set; }
        [Key(2)] public string GameServerAddress { get; set; } = string.Empty;
        [Key(3)] public int GameServerPort { get; set; }
        [Key(4)] public long OwnerUserId { get; set; }
        [Key(5)] public List<PlayerInfo> Players { get; set; } = [];
    }
}
