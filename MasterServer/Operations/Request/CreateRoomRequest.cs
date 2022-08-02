using MasterServer.DB.Table;
using MessagePack;
using System.Collections;

namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class CreateRoomRequest
    {
        [Key(0)]
        public int UserID;
        [Key(1)]
        public string RoomName = "";
        [Key(2)]
        public bool IsVisible;
        [Key(3)]
        public bool NeedPassword;
        [Key(4)]
        public string Password = "";
        [Key(5)]
        public int MaxPlayers;
        [Key(6)]
        public Hashtable CustomProperties = new Hashtable();
    }

    [MessagePackObject]
    public class CreateRoomResponse
    {
        [Key(0)]
        public RoomProperty RoomProperty;
        [Key(1)]
        public List<UserTable> Users;
    }
}
