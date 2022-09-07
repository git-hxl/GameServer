using MessagePack;
namespace GameServer.Request
{
    [MessagePackObject]
    public class AuthRequest
    {
        [Key(0)]
        public string Token;
        [Key(1)]
        public string AppID;
        [Key(2)]
        public string AppVersion;
    }

    [MessagePackObject]
    public class AuthResponse
    {
        [Key(0)]
        public string UserID;
        [Key(1)]
        public string NickName;
    }
}
