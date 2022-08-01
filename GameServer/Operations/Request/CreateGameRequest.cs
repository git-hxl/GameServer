using MessagePack;
using System.Collections;

namespace GameServer.Operations.Request
{
    [MessagePackObject]
    public class CreateGameRequest
    {
        [Key(0)]
        public string GameID = "";
    }

    [MessagePackObject]
    public class CreateGameResponse
    {
        [Key(0)]
        public string GameID = "";
    }
}
