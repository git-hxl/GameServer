using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class GameReadyNotify
    {
        [Key(0)] public string RoomId { get; set; } = string.Empty;
        [Key(1)] public long UserId { get; set; }
        [Key(2)] public bool IsReady { get; set; }
        [Key(3)] public int ReadyCount { get; set; }
        [Key(4)] public int TotalCount { get; set; }
    }
}
