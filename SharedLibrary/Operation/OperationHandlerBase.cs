using LiteNetLib;

namespace SharedLibrary
{
    public abstract class OperationHandlerBase
    {
        public abstract void OnRequest(BasePeer basePeer, OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod);
        public abstract void OnResponse(BasePeer basePeer, OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod);
    }
}
