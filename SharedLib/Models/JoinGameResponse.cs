using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class JoinGameResponse
    {
        [Key(0)] public string RoomId { get; set; } = string.Empty;
        [Key(1)] public RoomType RoomType { get; set; }
        [Key(2)] public long OwnerUserId { get; set; }
        [Key(3)] public List<PlayerInfo> Players { get; set; } = [];
    }
}
