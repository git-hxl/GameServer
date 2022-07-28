using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class LeaveLobbyRequest
    {
        [Key(0)]
        public string LobbyID = "";
    }
}
