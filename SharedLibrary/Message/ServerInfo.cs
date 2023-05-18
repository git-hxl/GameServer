using MessagePack;
namespace SharedLibrary.Message
{
    [MessagePackObject]
    public class ServerInfo
    {
        [Key(0)]
        public int Players { get; set; }
        [Key(1)]
        public int CPU { get; set; }
        [Key(2)]
        public int Memory { get; set; }

    }
}
