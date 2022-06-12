using CommonLibrary.Operations;
using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class LoginRequest : RequestBase
    {
        public string Account { get; set; }
        public string Password { get; set; }
    }
}
