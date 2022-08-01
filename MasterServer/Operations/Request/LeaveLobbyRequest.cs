using MessagePack;

namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class LeaveLobbyRequest
    {
        [Key(0)]
        public int UserID;
        [Key(1)]
        public string LobbyID = "";
    }

    [MessagePackObject]
    public class LeaveLobbyResponse
    {
        [Key(0)]
        public int UserID;
        [Key(1)]
        public string LobbyID = "";
    }
}
