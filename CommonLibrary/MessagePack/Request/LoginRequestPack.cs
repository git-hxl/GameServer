using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class LoginRequestPack : RequsetBasePack
    {
        [Key(2)]
        public string Account = "";
        [Key(3)]
        public string Password = "";
    }
}
