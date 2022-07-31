using MessagePack;
using System.Collections;

namespace GameServer.Operations.Request
{
    [MessagePackObject]
    public class CreateGameRequest
    {
        [Key(0)]
        public string RoomID = "";
    }
}
