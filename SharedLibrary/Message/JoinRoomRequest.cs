
using MessagePack;

namespace SharedLibrary.Message
{
    [MessagePackObject]
    public class JoinRoomRequest
    {
        [Key(0)]
        public string RoomID { get;set; }

        [Key(1)]
        public string RoomPassword { get;set; }

        [Key(2)]
        public UserInfo UserInfo { get;set; }
    }

    [MessagePackObject]
    public class JoinRoomResponse
    {
        [Key(0)]
        public RoomInfo RommInfo { get; set; }

        [Key(1)]
        public List<UserInfo> UserInfos { get; set; }
    }
}
