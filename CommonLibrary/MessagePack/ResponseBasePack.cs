using CommonLibrary.Operations;
using CommonLibrary.Utils;
using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject(true)]
    public class ResponseBasePack
    {
        public ReturnCode ReturnCode { get; set; }
        public string DebugMsg { get; set; } = "";
        public long TimeStamp { get; set; }

        public ResponseBasePack()
        {
            this.TimeStamp = DateTimeEx.TimeStamp;
        }
    }
}
