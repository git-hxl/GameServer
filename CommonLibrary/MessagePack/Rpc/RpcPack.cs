using MessagePack;
using System.Collections;

namespace CommonLibrary.MessagePack
{
    [MessagePackObject]
    public class RpcPack : RequsetBasePack
    {
        [Key(2)]
        public string RoomID = "";
        [Key(3)]
        public int UserID;
        [Key(4)]
        public string MethodName = "";
        [Key(5)]
        public Hashtable Param = new Hashtable();
    }
}
