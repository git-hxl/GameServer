using CommonLibrary.MessagePack.Operation;
using CommonLibrary.Utils;
using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class MsgPack
    {
        [Key(0)]
        public OperationCode OperationCode;
        [Key(1)]
        public long TimeStamp;
        [Key(2)]
        public byte[] Data = new byte[0];
    }
}