using LiteNetLib;
using SharedLibrary.Server;

namespace SharedLibrary.Operation
{
    public abstract class OperationHandlerBase
    {
        public abstract void OnRequest(OperationCode operationCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod);
        public abstract void OnResponse(OperationCode operationCode, ReturnCode returnCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod);
    }
}
