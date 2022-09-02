using MessagePack;
using System.Collections;

namespace MasterServer
{
    [MessagePackObject]
    public class CreateRoomRequest
    {
        [Key(0)]
        public string UserID;
        [Key(1)]
        public string RoomName;
        [Key(2)]
        public int MaxPeers;
        [Key(3)]
        public bool IsVisible;
        [Key(4)]
        public string Password;

        [Key(5)]
        public Hashtable RoomProperties;
    }

    [MessagePackObject]
    public class CreateRoomResponse
    {
        [Key(0)]
        public string RoomID;
        [Key(1)]
        public string GameIP;
        [Key(2)]
        public int GamePort;
    }
}
