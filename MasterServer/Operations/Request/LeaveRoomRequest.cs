using MessagePack;
namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class LeaveRoomRequest
    {
        [Key(0)]
        public string RoomID = "";
    }
}
