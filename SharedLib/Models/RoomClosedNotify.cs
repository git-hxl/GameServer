using MessagePack;

namespace SharedLib.Models;

[MessagePackObject]
public class RoomClosedNotify
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public RoomCloseReason Reason { get; set; }
}
