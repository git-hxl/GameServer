using MessagePack;
using System.Collections;

namespace GameServer.Operations.Request
{
    [MessagePackObject]
    public class JoinGameRequest
    {
        [Key(0)]
        public string RoomID = "";
        [Key(1)]
        public int UserID;
    }
}
