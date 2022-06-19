using MessagePack;

namespace MasterServer.Operations
{
    [MessagePackObject(true)]
    public class AuthHandleResponse
    {
        public string Nickname { get; set; } = "";
        public string UserId { get; set; } = "";
        public string Endpoint { get; set; } = "";
        public long TimeStamp { get; set; }
    }
}
