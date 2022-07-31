using MessagePack;
namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class JoinLobbyRequest
    {
        [Key(0)]
        public string LobbyID = "";
    }
}
