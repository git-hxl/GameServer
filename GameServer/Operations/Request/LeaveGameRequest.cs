using MessagePack;
using System.Collections;

namespace GameServer.Operations.Request
{
    [MessagePackObject]
    public class LeaveGameRequest
    {
        [Key(0)]
        public string GameID = "";
        [Key(1)]
        public int UserID;
    }

    [MessagePackObject]
    public class LeaveGameResponse
    {
        [Key(0)]
        public string GameID = "";
        [Key(1)]
        public int UserID;
    }
}
