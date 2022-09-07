using MessagePack;
namespace GameServer.Request
{
    [MessagePackObject]
    public class RpcRequest
    {
        [Key(0)]
        public string ObjectID;
        [Key(1)]
        public string MethodName;
        [Key(2)]
        public object[] Params;
    }
}
