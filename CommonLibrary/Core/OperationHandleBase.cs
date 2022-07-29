using CommonLibrary.MessagePack;
using CommonLibrary.MessagePack.Operation;
using LiteNetLib;
namespace CommonLibrary.Core
{
    public abstract class OperationHandleBase
    {
        public OperationHandleBase() { }
        public abstract void HandleRequest(NetPeer netPeer, MsgPack msgPack, DeliveryMethod deliveryMethod);
    }
}
