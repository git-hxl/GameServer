using CommonLibrary.Core;
using LiteNetLib;

namespace CommonLibrary.Core
{
    public class HandleRequest
    {
        public NetPeer Peer;
        public MsgPack MsgPack;
        public DeliveryMethod DeliveryMethod;
        public HandleRequest(NetPeer netPeer, MsgPack msgPack, DeliveryMethod deliveryMethod)
        {
            Peer = netPeer;
            MsgPack = msgPack;
            DeliveryMethod = deliveryMethod;
        }
    }
}
