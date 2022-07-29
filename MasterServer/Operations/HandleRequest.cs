using CommonLibrary.MessagePack;
using CommonLibrary.MessagePack.Operation;
using LiteNetLib;

namespace MasterServer.Operations
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
