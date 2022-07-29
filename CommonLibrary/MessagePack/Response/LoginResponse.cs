using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class LoginResponse
    {
        [Key(0)]
        public int ID;
        [Key(1)]
        public string NickName = "";
    }
}