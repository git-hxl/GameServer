using MessagePack;

namespace SharedLibrary.Model
{
    [MessagePackObject]
    public class UserInfo
    {
        [Key(0)]
        public string UID { get; set; }
        [Key(1)]
        public string Account { get; set; }
        [Key(2)]
        public string Password { get; set; }
    }
}