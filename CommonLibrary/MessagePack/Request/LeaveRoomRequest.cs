using MessagePack;
namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class LeaveRoomRequest
    {
        [Key(0)]
        public string RoomID = "";
    }
}
