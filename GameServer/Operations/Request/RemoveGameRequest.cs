using MessagePack;
using System.Collections;

namespace GameServer.Operations.Request
{
    [MessagePackObject]
    public class RemoveGameRequest
    {
        [Key(0)]
        public string GameID = "";
    }

    [MessagePackObject]
    public class RemoveGameResponse
    {
        [Key(0)]
        public string GameID = "";
    }
}
