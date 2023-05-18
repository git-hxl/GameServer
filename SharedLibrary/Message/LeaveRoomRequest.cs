using MessagePack;

namespace SharedLibrary.Message
{
    [MessagePackObject]
    public class LeaveRoomRequest
    {
        [Key(0)]
        public string RoomID { get; set; }
    }

    [MessagePackObject]
    public class LeaveRoomResponse
    {
        [Key(0)]
        public string RoomID { get; set; }
        [Key(1)]
        public UserInfo UserInfo { get; set; }
    }
}
