using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class TestPack
    {
        public string dateTime { get; set; }
        public int id { get; set; }
    }
}
