using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject(true)]
    public class LoginResponsePack : ResponseBasePack
    {
        public int ID { get; set; }
        public string Token { get; set; } = "";
    }
}
