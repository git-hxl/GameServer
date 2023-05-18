using MessagePack;
using System.Net;

namespace SharedLibrary.Message
{
    [MessagePackObject]
    public class CreateRoomRequest
    {
        [Key(0)]
        public RoomInfo RoomInfo { get; set; }
    }

    [MessagePackObject]
    public class CreateRoomResponse
    {
        [Key(0)]
        public string RoomID { get; set; }
        [Key(1)]
        public string GameServer { get; set; }
    }

    [MessagePackObject(true)]
    public class RoomInfo
    {
        public string RoomID { get; set; }
        public string RoomName { get; set; }
        public string RoomDescription { get; set; }
        public int RoomType { get; set; }
        public string RoomPassword { get; set; }
        public int RoomMaxPlayers { get; set; }
        public string OwnerID { get; set; }
    }
}
