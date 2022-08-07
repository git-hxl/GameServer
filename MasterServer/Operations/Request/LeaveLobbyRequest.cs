using MessagePack;

namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class LeaveLobbyRequest
    {

    }

    [MessagePackObject]
    public class LeaveLobbyResponse
    {
        [Key(0)]
        public string LobbyID = "";
    }
}
