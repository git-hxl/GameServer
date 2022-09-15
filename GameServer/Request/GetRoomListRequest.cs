using GameServer.Client;
using GameServer.Room;
using MessagePack;
namespace GameServer.Request
{
    [MessagePackObject]
    public class GetRoomListRequest
    {

    }

    [MessagePackObject]
    public class GetRoomListResponse
    {
        [Key(0)]
        public List<RoomInfo> RoomInfos = new List<RoomInfo>();
    }
}