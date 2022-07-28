using MessagePack;
using System.Collections;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class CreateRoomRequest
    {
        [Key(0)]
        public bool IsVisible;
        [Key(1)]
        public bool NeedPassword;
        [Key(2)]
        public string Password = "";
        [Key(3)]
        public int MaxPlayers;
        [Key(4)]
        public Hashtable RoomProperties = new Hashtable();
    }
}
