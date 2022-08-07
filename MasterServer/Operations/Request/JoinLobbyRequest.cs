using MessagePack;
namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class JoinLobbyRequest
    {

    }

    [MessagePackObject]
    public class JoinLobbyResponse
    {
        [Key(0)]
        public string LobbyID = "";
        [Key(1)]
        public List<RoomProperty> Rooms;
    }
}
