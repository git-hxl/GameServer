using CommonLibrary.Operations;
using CommonLibrary.Utils;
using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject(true)]
    public class RequsetBasePack
    {
        public string Token { get; set; } = "";
        public long TimeStamp { get; set; }

        public RequsetBasePack()
        {
            this.TimeStamp = DateTimeEx.TimeStamp;
        }
    }
}