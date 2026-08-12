using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class AnimationSyncData
    {
        [Key(0)] public long EntityId { get; set; }
        [Key(1)] public uint TickId { get; set; }
        [Key(2)] public string AnimName { get; set; } = string.Empty;
        [Key(3)] public float AnimNormalTime { get; set; }
        [Key(4)] public Dictionary<string, int> IntParams { get; set; } = [];
        [Key(5)] public Dictionary<string, float> FloatParams { get; set; } = [];
        [Key(6)] public Dictionary<string, bool> BoolParams { get; set; } = [];
    }
}
