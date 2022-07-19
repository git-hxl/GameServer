using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class JoinRoomResponsePack : ResponseBasePack
    {
        [Key(1)]
        public string RoomID = "";
        [Key(2)]
        public string RoomName = "";
        [Key(3)]
        public List<int> Players = new List<int>();
    }
}
