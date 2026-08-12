using MessagePack;

namespace SharedLib.Models
{

    /// <summary>
    /// 离开房间响应
    /// </summary>
    [MessagePackObject]
    public class LeaveRoomResponse
    {
        [Key(0)] public string RoomId { get; set; } = string.Empty;
    }
}
