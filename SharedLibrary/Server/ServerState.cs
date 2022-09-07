using MessagePack;
namespace MasterServer.GameServer
{
    [MessagePackObject]
    public class ServerState
    {
        [Key(0)]
        public int RoomCount { get; set; }
        [Key(1)]
        public int PlayerCount { get; set; }
        [Key(2)]
        public int CPUPercent { get; set; }
        [Key(3)]
        public int MemoryPercent { get; set; }
    }
}
