using MessagePack;

namespace SharedLib.Models
{

    /// <summary>
    /// 离开大厅请求
    /// </summary>
    [MessagePackObject]
    public class LeaveLobbyRequest
    {
        [Key(0)] public long UserId { get; set; }
    }
}
