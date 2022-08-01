using MasterServer.DB.Table;
using MessagePack;

namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class LoginRequest
    {
        [Key(0)]
        public string Account = "";
        [Key(1)]
        public string Password = "";
    }

    [MessagePackObject]
    public class LoginResponse
    {
        [Key(0)]
        public UserTable User;
        [Key(1)]
        public List<LobbyProperty> Lobbies;
    }
}