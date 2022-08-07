using MessagePack;
using System.Collections;

namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class GetRoomListRequest
    {

    }

    [MessagePackObject]
    public class GetRoomListResponse
    {
        [Key(0)]
        public List<RoomProperty> Rooms;
    }
}
