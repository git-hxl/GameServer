
using MessagePack;

namespace MasterServer.Operation.Request
{
    [MessagePackObject]
    public class RegisterRequest
    {
        [Key(0)]
        public string Account;
        [Key(1)]
        public string Password;
    }

    [MessagePackObject]
    public class RegisterResponse
    {
        [Key(0)]
        public string UID;
        [Key(1)]
        public string NickName;
    }
}
