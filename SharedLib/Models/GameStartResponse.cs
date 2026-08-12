using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class GameStartResponse
    {
        [Key(0)] public int Code { get; set; }
    }
}
