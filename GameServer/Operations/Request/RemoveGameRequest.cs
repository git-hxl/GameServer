using MessagePack;
using System.Collections;

namespace GameServer.Operations.Request
{
    [MessagePackObject]
    public class RemoveGameRequest
    {
        [Key(0)]
        public string RoomID = "";
    }
}
