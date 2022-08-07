using MessagePack;
namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class LeaveRoomRequest
    {
    }

    [MessagePackObject]
    public class LeaveRoomResponse
    {
        [Key(0)]
        public int UserID;
        [Key(1)]
        public string RoomID;
    }
}
