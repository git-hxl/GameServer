using CommonLibrary.Utils;
using MessagePack;

namespace CommonLibrary.Core
{
    [MessagePackObject]
    public class MsgPack
    {
        [Key(0)]
        public byte[] Data;
        [Key(1)]
        public long TimeStamp;

        public MsgPack(byte[] data)
        {
            Data = data;
            TimeStamp = DateTimeEx.TimeStamp;
        }
    }
}