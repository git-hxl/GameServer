using MessagePack;
namespace GameServer.Request
{
    [MessagePackObject]
    public class LeaveRoomRequest
    {

    }

    [MessagePackObject]
    public class LeaveRoomResponse
    {
        [Key(0)]
        public string UserID;
    }
}