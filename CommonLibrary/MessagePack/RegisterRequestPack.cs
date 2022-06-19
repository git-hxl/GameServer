
using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject(true)]
    public class RegisterRequestPack
    {
        public string Account { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
