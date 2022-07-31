using MessagePack;

namespace MasterServer.Operations.Response
{
    [MessagePackObject]
    public class OnPlayerLeaveRoomResponse
    {
        [Key(0)]
        public int UserID;
    }
}