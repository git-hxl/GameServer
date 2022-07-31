using MessagePack;

namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class LeaveLobbyRequest
    {
        [Key(0)]
        public string LobbyID = "";
    }
}
