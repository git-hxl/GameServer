using MessagePack;

namespace SharedLibrary.Message
{
    [MessagePackObject]
    public class LoginRequest
    {
        [Key(0)]
        public string Account { get; set; }
        [Key(1)]
        public string Password { get; set; }
    }

    [MessagePackObject]
    public class LoginResponse
    {
        [Key(0)]
        public UserInfo UserInfo { get; set; }
    }
}
