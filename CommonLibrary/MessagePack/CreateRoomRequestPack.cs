
using MessagePack;
using System.Collections;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject(true)]
    public class CreateRoomRequestPack : RequsetBasePack
    {
        public string RoomName { get; set; }
        public bool IsVisible { get; set; }
        public string Password { get; set; }
        public int MaxPlayers { get; set; }
        public Hashtable RoomProperties { get; set; }
    }
}
