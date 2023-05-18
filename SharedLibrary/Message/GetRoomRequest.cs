using MessagePack;

namespace SharedLibrary.Message
{
    [MessagePackObject]
    public class GetRoomRequest
    {

    }

    [MessagePackObject]
    public class GetRoomResponse
    {
        [Key(0)]
        public List<RoomInfo> RoomInfos { get; set; }
    }
}
