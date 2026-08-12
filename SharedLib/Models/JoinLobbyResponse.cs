using MessagePack;

namespace SharedLib.Models
{

    /// <summary>
    /// 加入大厅响应
    /// </summary>
    [MessagePackObject]
    public class JoinLobbyResponse
    {
        [Key(0)] public PlayerInfo Player { get; set; } = new();
    }
}
