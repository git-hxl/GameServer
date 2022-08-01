using MessagePack;
using System.Collections;

namespace MasterServer.Operations.Request
{
    public class GetRoomListRequest
    {
        [Key(0)]
        public int UserID;
        [Key(1)]
        public string LobbyID = "";
    }

    [MessagePackObject]
    public class GetRoomListResponse
    {
        [Key(0)]
        public List<RoomProperty> Rooms;
    }
}
