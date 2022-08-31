using MessagePack;

namespace ShareLibrary
{
    [MessagePackObject]
    public class RegisterGameServerRequest
    {
        [Key(0)]
        public string EndPoint;
    }

    [MessagePackObject]
    public class RegisterGameServerResponse
    {
        [Key(0)]
        public string EndPoint;
    }
}
