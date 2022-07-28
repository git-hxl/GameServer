using MessagePack;
namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class JoinLobbyRequest
    {
        [Key(0)]
        public string LobbyID = "";
    }
}
