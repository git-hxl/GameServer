using MessagePack;

namespace SharedLib.Models
{

    /// <summary>
    /// 加入房间通知
    /// </summary>
    [MessagePackObject]
    public class JoinRoomNotify
    {
        [Key(0)] public string RoomId { get; set; } = string.Empty;
        [Key(1)] public PlayerInfo Player { get; set; } = new();
    }
}
