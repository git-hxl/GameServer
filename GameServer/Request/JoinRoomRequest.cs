using GameServer.Client;
using GameServer.Room;
using MessagePack;
namespace GameServer.Request
{
    [MessagePackObject]
    public class JoinRoomRequest
    {
        [Key(0)]
        public string RoomID;
        [Key(1)]
        public string Password;
    }

    [MessagePackObject]
    public class JoinRoomResponse
    {
        [Key(0)]
        public string UserID;
        [Key(1)]
        public RoomInfo RoomInfo;
        [Key(2)]
        public List<PlayerInfo> Players = new List<PlayerInfo>();
    }
}