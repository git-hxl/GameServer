using MessagePack;
using System.Collections;

namespace GameServer.Operations
{
    [MessagePackObject]
    public class RpcPack
    {
        [Key(0)]
        public string MethodName = "";
        [Key(1)]
        public Hashtable Param = new Hashtable();
    }
}
