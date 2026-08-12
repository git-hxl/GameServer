using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class GameUnreadyResponse
    {
        [Key(0)] public int ReadyCount { get; set; }
        [Key(1)] public int TotalCount { get; set; }
    }
}
