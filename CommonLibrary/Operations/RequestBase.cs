using MessagePack;

namespace CommonLibrary.Operations
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class RequestBase
    {
        public string Token { get; set; } = "";
        public long TimeStamp { get; set; }
    }
}
