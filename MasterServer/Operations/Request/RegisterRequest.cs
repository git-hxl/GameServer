using MessagePack;

namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class RegisterRequest
    {
        [Key(0)]
        public string Account = "";
        [Key(1)]
        public string Password = "";
        [Key(2)]
        public string NickName = "";
        [Key(3)]
        public string RealName = "";
        [Key(4)]
        public string Identity = "";
    }

    [MessagePackObject]
    public class RegisterResponse
    {
        [Key(0)]
        public string Account = "";
    }
}
