using MasterServer.DB.Table;
using MessagePack;

namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class JoinRoomRequest
    {
        [Key(0)]
        public string RoomID = "";
        [Key(1)]
        public string Password = "";
    }

    [MessagePackObject]
    public class JoinRoomResponse
    {
        [Key(0)]
        public int UserID;
        [Key(1)]
        public string RoomID;
        [Key(2)]
        public RoomProperty RoomProperty;
        [Key(3)]
        public List<UserTable> Users;
    }
}
