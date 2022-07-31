using MasterServer.DB.Table;
using MessagePack;

namespace MasterServer.Operations.Response
{
    [MessagePackObject]
    public class OnJoinRoomResponse
    {
        [Key(0)]
        public RoomProperty RoomProperty;
        [Key(1)]
        public List<UserTable> Users;
    }
}