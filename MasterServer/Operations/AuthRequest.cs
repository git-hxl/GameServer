using MessagePack;
using ShareLibrary;

namespace MasterServer.Operations
{
    [MessagePackObject]
    public class AuthRequest : DataBase
    {
        [Key(0)]
        public string Token;
        [Key(1)]
        public string AppID;
        [Key(2)]
        public string AppVersion;
    }

    [MessagePackObject]
    public class AuthResponse : DataBase
    {
        [Key(0)]
        public string UserID;
        [Key(1)]
        public string NickName;
    }
}
