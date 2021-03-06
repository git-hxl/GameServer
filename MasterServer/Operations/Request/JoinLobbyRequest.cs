using MessagePack;
namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class JoinLobbyRequest
    {
        [Key(0)]
        public int UserID;
        [Key(1)]
        public string LobbyID = "";
    }

    [MessagePackObject]
    public class JoinLobbyResponse
    {
        [Key(0)]
        public int UserID;
        [Key(1)]
        public string LobbyID = "";
        [Key(2)]
        public List<RoomProperty> Rooms;
    }
}
