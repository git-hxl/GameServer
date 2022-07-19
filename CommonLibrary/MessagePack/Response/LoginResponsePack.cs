using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class LoginResponsePack : ResponseBasePack
    {
        [Key(1)]
        public int ID;
        [Key(2)]
        public string Token = "";
    }
}
