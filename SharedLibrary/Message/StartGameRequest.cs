using MessagePack;
using System.Net;

namespace SharedLibrary.Message
{
    [MessagePackObject]
    public class StartGameRequest
    {
        [Key(0)]
        public string UID { get; set; }
        [Key(1)]
        public string RoomID { get; set; }
    }

    [MessagePackObject]
    public class StartGameResponse
    {
        [Key(0)]
        public EndPoint GameEndPoint { get; set; }
    }
}
