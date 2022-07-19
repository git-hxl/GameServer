using CommonLibrary.Utils;
using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class ResponseBasePack
    {
        [Key(0)]
        public long TimeStamp;

        public ResponseBasePack()
        {
            TimeStamp = DateTimeEx.TimeStamp;
        }
    }
}
