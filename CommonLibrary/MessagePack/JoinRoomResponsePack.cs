
using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject(true)]
    public class JoinRoomResponsePack
    {
        public string RoomID { get; set; }
        public string RoomName { get; set; }
        public int OwnerID { get; set; }
        public List<int> Players = new List<int>();
    }
}
