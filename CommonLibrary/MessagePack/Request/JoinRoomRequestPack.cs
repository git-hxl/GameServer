using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class JoinRoomRequestPack : RequsetBasePack
    {
        [Key(2)]
        public string RoomID = "";
        [Key(3)]
        public string Password = "";
    }
}
