using MessagePack;

namespace SharedLib.Models
{

    /// <summary>
    /// 房间列表信息项
    /// </summary>
    [MessagePackObject]
    public class RoomListInfo
    {
        [Key(0)] public string RoomId { get; set; } = string.Empty;
        [Key(1)] public RoomType RoomType { get; set; }
        [Key(2)] public int PlayerCount { get; set; }
    }
}
