using MessagePack;
namespace MasterServer.MasterClient.Request
{
    [MessagePackObject]
    public class LeaveLobbyRequest
    {
        [Key(0)]
        public string UserID;
        [Key(1)]
        public string LobbyName;
    }

    [MessagePackObject]
    public class LeaveLobbyResponse
    {
        [Key(0)]
        public string LobbyName;
    }
}
