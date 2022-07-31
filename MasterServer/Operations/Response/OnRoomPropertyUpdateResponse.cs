using MessagePack;

namespace MasterServer.Operations.Response
{
    [MessagePackObject]
    public class OnRoomPropertyUpdateResponse
    {
        [Key(0)]
        public RoomProperty RoomProperty;
    }
}