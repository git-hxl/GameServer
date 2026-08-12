using MessagePack;

namespace SharedLib.Models
{

    [MessagePackObject]
    public class ObjectDespawnData
    {
        [Key(0)] public long ObjectId { get; set; }
    }
}
