using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class GameServerInfo
    {
        [Key(0)] public string Address { get; set; } = string.Empty;
        [Key(1)] public int Port { get; set; }
        [Key(2)] public int PlayerCount { get; set; }
        [Key(3)] public int RoomCount { get; set; }
        [Key(4)] public float CpuPercent { get; set; }
        [Key(5)] public long MemoryMB { get; set; }
    }
}
