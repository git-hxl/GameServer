
using LiteNetLib;
using MessagePack;
using SharedLibrary;

namespace HotLibrary
{
    public class HotOperationHandler : OperationHandlerBase
    {
        public override void OnRequest(BasePeer basePeer, OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            //Log.Information("Hot OnRequest :{0} {1}", basePeer.NetPeer.Id, operationCode);
        }



        public override void OnResponse(BasePeer basePeer, OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            //Log.Information("Hot OnResponse :{0} {1}", basePeer.NetPeer.Id, operationCode);
        }
    }
}
