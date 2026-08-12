using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class JoinGameNotify
    {
        [Key(0)] public string RoomId { get; set; } = string.Empty;
        [Key(1)] public PlayerInfo Player { get; set; } = new();
    }
}
