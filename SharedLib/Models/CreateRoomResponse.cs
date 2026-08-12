using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class CreateRoomResponse
    {
        [Key(0)] public RoomInfo Room { get; set; } = new();
    }
}
