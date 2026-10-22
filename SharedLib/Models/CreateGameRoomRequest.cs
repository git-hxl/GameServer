using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class CreateGameRoomRequest
    {
        [Key(0)] public string RoomId { get; set; } = string.Empty;
        [Key(1)] public RoomType RoomType { get; set; }
        [Key(2)] public int MaxPlayers { get; set; }
    }
}
