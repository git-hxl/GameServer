using MessagePack;

namespace MasterServer.Operations.Response
{

    [MessagePackObject]
    public class UpdateRoomPropertyRequest
    {
        [Key(0)]
        public string RoomID;
        [Key(1)]
        public RoomProperty RoomProperty;
    }

    [MessagePackObject]
    public class UpdateRoomPropertyResponse
    {
        [Key(0)]
        public string RoomID;
        [Key(1)]
        public RoomProperty RoomProperty;
    }
}