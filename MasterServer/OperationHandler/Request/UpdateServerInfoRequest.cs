using MessagePack;

namespace MasterServer
{
    [MessagePackObject]
    public class UpdateServerInfoRequest
    {
        [Key(0)]
        public int RoomCount;
        [Key(1)]
        public int PlayerCount;
        [Key(2)]
        public int CPUPercent;
        [Key(3)]
        public int MemoryPercent;
    }
}
