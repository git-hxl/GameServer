
using MessagePack;

namespace SharedLibrary.Model
{
    [MessagePackObject]
    public class ServerInfo
    {
        [Key(0)]
        public int CurPlayers { get; set; }
        [Key(1)]
        public int MaxPlayers { get; set; }
        [Key(2)]
        public int CurRooms { get; set; }
        [Key(3)]
        public int MaxRooms { get; set; }
        [Key(4)]
        public RoomInfo[] RoomInfos { get; set; }
        [Key(5)]
        public double CPUPercent { get; set; }
        [Key(6)]
        public double MemoryPercent { get; set; }
    }
}