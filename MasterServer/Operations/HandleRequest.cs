using CommonLibrary.Operations;
namespace MasterServer.Operations
{
    public class HandleRequest
    {
        public MasterPeer ClientPeer;
        public OperationCode OperationCode;
        public byte[] RequestData;

        public HandleRequest(MasterPeer clientPeer, OperationCode operationCode, byte[] requestData)
        {
            ClientPeer = clientPeer;
            OperationCode = operationCode;
            RequestData = requestData;
        }
    }
}
