using MessagePack;
namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class JoinLobbyRequestPack : RequsetBasePack
    {
        [Key(2)]
        public string LobbyName = "";
    }
}
