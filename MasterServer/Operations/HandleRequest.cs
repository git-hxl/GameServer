using CommonLibrary.Operations;
using LiteNetLib;

namespace MasterServer.Operations
{
    public class HandleRequest
    {
        public MasterPeer ClientPeer;
        public OperationCode OperationCode;
        public byte[] RequestData;
        public DeliveryMethod DeliveryMethod;
        public HandleRequest(MasterPeer clientPeer, OperationCode operationCode, byte[] requestData, DeliveryMethod deliveryMethod)
        {
            ClientPeer = clientPeer;
            OperationCode = operationCode;
            RequestData = requestData;
            DeliveryMethod = deliveryMethod;
        }
    }
}
