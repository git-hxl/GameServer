using MessagePack;
using System.Collections;

namespace GameServer.Request
{
    [MessagePackObject]
    public class CreateRoomRequest
    {
        [Key(0)]
        public string RoomName;
        [Key(1)]
        public int MaxPeers;
        [Key(2)]
        public bool IsVisible;
        [Key(3)]
        public string Password;
        [Key(4)]
        public Hashtable RoomProperties;
    }

    [MessagePackObject]
    public class CreateRoomResponse
    {
        [Key(0)]
        public string RoomID;
    }
}