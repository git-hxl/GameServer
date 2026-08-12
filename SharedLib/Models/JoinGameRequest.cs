using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class JoinGameRequest
    {
        [Key(0)] public string RoomId { get; set; } = string.Empty;
        [Key(1)] public PlayerInfo Player { get; set; } = new();
    }
}
