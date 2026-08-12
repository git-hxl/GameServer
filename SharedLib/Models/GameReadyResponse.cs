using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class GameReadyResponse
    {
        [Key(0)] public int ReadyCount { get; set; }
        [Key(1)] public int TotalCount { get; set; }
        [Key(2)] public bool AllReady { get; set; }
    }
}
