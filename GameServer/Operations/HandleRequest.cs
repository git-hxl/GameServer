
using LiteNetLib;

namespace GameServer.Operations
{
    internal class HandleRequest
    {
        public GamePeer GamePeer;
        public GameOperationCode OperationCode;
        public byte[] RequestData;
        public DeliveryMethod DeliveryMethod;
        public HandleRequest(GamePeer clientPeer, GameOperationCode operationCode, byte[] requestData, DeliveryMethod deliveryMethod)
        {
            GamePeer = clientPeer;
            OperationCode = operationCode;
            RequestData = requestData;
            DeliveryMethod = deliveryMethod;
        }
    }
}
