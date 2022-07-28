using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class JoinRoomRequest
    {
        [Key(0)]
        public string RoomID = "";
        [Key(1)]
        public string Password = "";
    }
}
