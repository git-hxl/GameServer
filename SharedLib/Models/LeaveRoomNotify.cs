using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class LeaveRoomNotify
    {
        [Key(0)] public string RoomId { get; set; } = string.Empty;
        [Key(1)] public long UserId { get; set; }
    }
}
