using CommonLibrary.Core;
using LiteNetLib;

namespace CommonLibrary.Core
{
    public class HandleRequest
    {
        public NetPeer NetPeer;
        public OperationCode OperationCode;
        public MsgPack MsgPack;
        public DeliveryMethod DeliveryMethod;
        public HandleRequest(NetPeer netPeer, OperationCode operationCode,MsgPack msgPack, DeliveryMethod deliveryMethod)
        {
            NetPeer = netPeer;
            OperationCode = operationCode;
            MsgPack = msgPack;
            DeliveryMethod = deliveryMethod;
        }
    }
}
