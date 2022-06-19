using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject(true)]
    public class LoginRequestPack : RequsetBasePack
    {
        public string Account { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
