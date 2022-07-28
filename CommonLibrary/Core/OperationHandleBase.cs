using CommonLibrary.MessagePack.Operation;
using LiteNetLib;
namespace CommonLibrary.Core
{
    public abstract class OperationHandleBase
    {
        public OperationHandleBase() { }
        public abstract void HandleRequest(OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod);
    }
}
