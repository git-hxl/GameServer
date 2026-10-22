using MessagePack;

namespace SharedLib.Models;

[MessagePackObject]
public class CloseGameRoomRequest
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public RoomCloseReason Reason { get; set; }
}
