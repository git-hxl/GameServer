using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class LeaveGameResponse
    {
        [Key(0)] public string RoomId { get; set; } = string.Empty;
    }
}
