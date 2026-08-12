using MessagePack;

namespace SharedLib.Models
{

    /// <summary>
    /// 房间列表响应
    /// </summary>
    [MessagePackObject]
    public class RoomListResponse
    {
        [Key(0)] public List<RoomListInfo> Rooms { get; set; } = [];
    }
}
