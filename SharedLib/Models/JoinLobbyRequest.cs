using MessagePack;

namespace SharedLib.Models
{

    /// <summary>
    /// 加入大厅请求
    /// </summary>
    [MessagePackObject]
    public class JoinLobbyRequest
    {
        [Key(0)] public PlayerInfo Player { get; set; } = new();
    }
}
