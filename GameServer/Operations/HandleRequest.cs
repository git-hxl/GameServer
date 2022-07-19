
using LiteNetLib;

namespace GameServer.Operations
{
    internal class HandleRequest
    {
        public NetPeer Peer;
        public GameOperationCode OperationCode;
        public byte[] RequestData;

        public HandleRequest(NetPeer peer, GameOperationCode operationCode, byte[] requestData)
        {
            Peer = peer;
            OperationCode = operationCode;
            RequestData = requestData;
        }
    }
}
