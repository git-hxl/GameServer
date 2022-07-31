using MasterServer.DB.Table;
using MessagePack;

namespace MasterServer.Operations.Response
{
    [MessagePackObject]
    public class OnLoginResponse
    {
        [Key(0)]
        public UserTable UserTable;
        [Key(1)]
        public List<LobbyProperty> Lobbies;
    }
}