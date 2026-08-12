using MessagePack;

namespace SharedLib.Models
{

    /// <summary>
    /// 离开大厅响应
    /// </summary>
    [MessagePackObject]
    public class LeaveLobbyResponse
    {
        [Key(0)] public long UserId { get; set; }
    }
}
