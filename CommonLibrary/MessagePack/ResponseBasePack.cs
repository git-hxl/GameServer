using CommonLibrary.Operations;
using CommonLibrary.Utils;
using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject(true)]
    public class ResponseBasePack
    {
        public long TimeStamp { get; set; }

        public ResponseBasePack()
        {
            this.TimeStamp = DateTimeEx.TimeStamp;
        }
    }
}
