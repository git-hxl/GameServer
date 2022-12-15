
using MessagePack;

namespace MasterServer.Operation.Request
{
    [MessagePackObject]
    public class LoginRequest
    {
        [Key(0)]
        public string Account;
        [Key(1)]
        public string Password;
    }

    [MessagePackObject]
    public class LoginResponse
    {
        [Key(0)]
        public string UID;
        [Key(1)]
        public string NickName;
    }
}
