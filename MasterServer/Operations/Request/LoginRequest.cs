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
}
