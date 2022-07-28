using MessagePack;

namespace CommonLibrary.MessagePack
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
