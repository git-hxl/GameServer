using CommonLibrary.Operations;
using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class LoginResponse: ResponseBase
    {
        public string ID { get; set; } = "";
    }
}
