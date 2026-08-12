using MessagePack;

namespace SharedLib.Models
{

    /// <summary>
    /// 玩家基本信息
    /// </summary>
    [MessagePackObject]
    public class PlayerInfo
    {
        [Key(0)] public long UserId { get; set; }
        [Key(1)] public string Nickname { get; set; } = string.Empty;
        [Key(2)] public int Gender { get; set; }
        [Key(3)] public string Avatar { get; set; } = string.Empty;
        [Key(4)] public int Age { get; set; }
        [Key(5)] public Dictionary<string, string> ExtraData { get; set; } = new();
    }
}
