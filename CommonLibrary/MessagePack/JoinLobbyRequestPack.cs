using MessagePack;
namespace CommonLibrary.MessagePack
{
    [MessagePackObject(true)]
    public class JoinLobbyRequestPack
    {
        public string LobbyName;
    }
}
