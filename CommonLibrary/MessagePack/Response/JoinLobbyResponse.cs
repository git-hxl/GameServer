using MessagePack;
using System.Collections;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class Room
    {
        [Key(0)]
        public string RoomName = "";
        [Key(1)]
        public string RoomID = "";
        [Key(2)]
        public int CurPlayers;
        [Key(3)]
        public int MaxPlayers;
        [Key(4)]
        public bool NeedPassword;
        [Key(5)]
        public Hashtable RoomProperties = new Hashtable();
    }

    [MessagePackObject]
    public class JoinLobbyResponse
    {
        [Key(0)]
        public string LobbyID = "";
        [Key(1)]
        public List<Room> Rooms = new List<Room>();
    }
}
