using CommonLibrary.Operations;
namespace MasterServer.Operations
{
    public class HandleRequest
    {
        public ClientPeer ClientPeer;
        public OperationCode OperationCode;
        public byte[] RequestData;

        public HandleRequest(ClientPeer clientPeer, OperationCode operationCode, byte[] requestData)
        {
            ClientPeer = clientPeer;
            OperationCode = operationCode;
            RequestData = requestData;
        }
    }
}
