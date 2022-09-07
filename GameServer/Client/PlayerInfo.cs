using MessagePack;

namespace GameServer.Client
{
    [MessagePackObject]
    public class PlayerInfo
    {
        [Key(0)]
        public string UserID { get; set; }

        [Key(1)]
        public string NickName { get; set; }

        [Key(2)]
        public int TeamID { get; set; }

    }
}
