using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class JoinRoomResponse
    {
        [Key(0)] public RoomInfo Room { get; set; } = new();
    }
}
