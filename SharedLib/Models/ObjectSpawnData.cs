using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class ObjectSpawnData
    {
        [Key(0)] public long ObjectId { get; set; }
        [Key(1)] public string PrefabName { get; set; } = string.Empty;
        [Key(2)] public float PosX { get; set; }
        [Key(3)] public float PosY { get; set; }
        [Key(4)] public float PosZ { get; set; }
        [Key(5)] public float RotX { get; set; }
        [Key(6)] public float RotY { get; set; }
        [Key(7)] public float RotZ { get; set; }
    }
}
