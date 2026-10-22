using MessagePack;

namespace SharedLib.Models;

[MessagePackObject]
public class RoomOwnerChangedNotify
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public long OwnerUserId { get; set; }
}
