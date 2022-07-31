using MessagePack;
using System.Collections;

namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class CreateRoomRequest
    {
        [Key(0)]
        public string RoomName = "";
        [Key(1)]
        public bool IsVisible;
        [Key(2)]
        public bool NeedPassword;
        [Key(3)]
        public string Password = "";
        [Key(4)]
        public int MaxPlayers;
        [Key(5)]
        public Hashtable CustomProperties = new Hashtable();
    }
}
