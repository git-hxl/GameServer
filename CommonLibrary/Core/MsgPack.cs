using CommonLibrary.Utils;
using MessagePack;

namespace CommonLibrary.Core
{
    [MessagePackObject]
    public class MsgPack
    {
        [Key(0)]
        public byte[]? Data;
        [Key(1)]
        public long TimeStamp;
        [Key(2)]
        public ReturnCode ReturnCode;

        public MsgPack(){}

        public MsgPack(byte[]? data, ReturnCode returnCode)
        {
            Data = data;
            TimeStamp = DateTimeEx.TimeStamp;
            ReturnCode = returnCode;
        }

        public static MsgPack Pack(byte[]? data, ReturnCode returnCode = ReturnCode.Success)
        {
            return new MsgPack(data, returnCode);
        }

        public static MsgPack Pack(ReturnCode returnCode)
        {
            return new MsgPack(null, returnCode);
        }

    }

}