
using MessagePack;

namespace SharedLibrary
{
    [MessagePackObject]
    public class HotLoadRequest
    {
        [Key(0)]
        public string Version {  get; set; }
    }
}
