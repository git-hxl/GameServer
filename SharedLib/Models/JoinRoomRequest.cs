using MessagePack;

namespace SharedLib.Models
{

    /// <summary>
    /// 加入房间请求
    /// </summary>
    [MessagePackObject]
    public class JoinRoomRequest
    {
        [Key(0)] public string RoomId { get; set; } = string.Empty;
    }
}
