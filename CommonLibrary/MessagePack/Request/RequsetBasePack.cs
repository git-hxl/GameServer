using CommonLibrary.Operations;
using CommonLibrary.Utils;
using MessagePack;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class RequsetBasePack
    {
        [Key(0)]
        public string Token = "";
        [Key(1)]
        public long TimeStamp;

        public RequsetBasePack()
        {
            TimeStamp = DateTimeEx.TimeStamp;
        }
    }
}