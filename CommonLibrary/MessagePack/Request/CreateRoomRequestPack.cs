using MessagePack;
using System.Collections;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class CreateRoomRequestPack : RequsetBasePack
    {
        [Key(2)]
        public string RoomName = "";
        [Key(3)]
        public bool IsVisible;
        [Key(4)]
        public string Password = "";
        [Key(5)]
        public int MaxPlayers;
        [Key(6)]
        public Hashtable RoomProperties = new Hashtable();
    }
}
