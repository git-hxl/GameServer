using MessagePack;

namespace SharedLibrary.Message
{
    [MessagePackObject]
    public class RegisterRequest
    {
        [Key(0)]
        public string Account { get; set; }
        [Key(1)]
        public string Password { get; set; }
        [Key(3)]
        public UserInfo UserInfo { get; set; }
    }

    [MessagePackObject]
    public class RegisterResponse
    {
        [Key(0)]
        public string UID { get; set; }
    }
}
