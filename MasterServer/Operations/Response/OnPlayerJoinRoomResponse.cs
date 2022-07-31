using MessagePack;

namespace MasterServer.Operations.Response
{
    [MessagePackObject]
    public class OnPlayerJoinRoomResponse
    {
        [Key(0)]
        public int UserID;
    }
}