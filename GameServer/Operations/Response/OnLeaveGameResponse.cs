using MessagePack;
namespace GameServer.Operations.Response
{
    [MessagePackObject]
    public class OnLeaveGameResponse
    {
        [Key(0)]
        public string RoomID = "";
        [Key(1)]
        public int UserID;
    }
}