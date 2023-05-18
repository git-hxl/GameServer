using MessagePack;
namespace SharedLibrary.Message
{
    [MessagePackObject(true)]
    public class UserInfo
    {
        public string UID { get; set; }
        public string NickName { get; set; }
    }
}
