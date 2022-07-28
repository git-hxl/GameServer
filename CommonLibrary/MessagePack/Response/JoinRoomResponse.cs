using MessagePack;
using System.Collections;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class Player
    {
        [Key(0)]
        public int ID;
        [Key(1)]
        public string NickName = "";
    }

    [MessagePackObject]
    public class JoinRoomResponse
    {
        [Key(0)]
        public string RoomID = "";
        [Key(1)]
        public List<Player> Players = new List<Player>();
        [Key(2)]
        public Hashtable RoomProperties = new Hashtable();
    }
}
