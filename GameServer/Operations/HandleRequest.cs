
using LiteNetLib;

namespace GameServer.Operations
{
    internal class HandleRequest
    {
        public NetPeer ClientPeer;
        public GameOperationCode OperationCode;
        public byte[] RequestData;

        public HandleRequest(NetPeer clientPeer, GameOperationCode operationCode, byte[] requestData)
        {
            ClientPeer = clientPeer;
            OperationCode = operationCode;
            RequestData = requestData;
        }
    }
}
