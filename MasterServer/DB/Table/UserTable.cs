using MessagePack;

namespace MasterServer.DB.Table
{
    [MessagePackObject]
    public class UserTable
    {
        [Key(0)]
        public int ID;
        [Key(1)]
        public string Account = "";
        [IgnoreMember]
        public string Password = "";
        [Key(3)]
        public string NickName = "";
        [Key(4)]
        public string Identity = "";
        [Key(5)]
        public DateTime LastLoginTime;
    }
}