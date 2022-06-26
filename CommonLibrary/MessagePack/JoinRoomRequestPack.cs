using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject(true)]
    public class JoinRoomRequestPack
    {
        public string RoomID { get; set; }
        public string Password { get; set; }
    }
}
