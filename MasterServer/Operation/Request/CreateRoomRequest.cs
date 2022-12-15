
using MessagePack;

namespace MasterServer.Operation.Request
{
    [MessagePackObject]
    public class CreateRoomRequest
    {
        [Key(0)]
        public string UID;
        [Key(1)]
        public int RoomType;
        [Key(2)]
        public string RoomName;
        [Key(3)]
        public string RoomPassword;
        [Key(4)]
        public int RoomMaxPlayers;
    }

    [MessagePackObject]
    public class CreateRoomResponse
    {
        [Key(0)]
        public string UID;
        [Key(1)]
        public string RoomID;
    }
}
