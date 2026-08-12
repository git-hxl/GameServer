using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class LeaveGameNotify
    {
        [Key(0)] public long UserId { get; set; }
    }
}
