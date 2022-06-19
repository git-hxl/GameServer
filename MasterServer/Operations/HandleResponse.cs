using CommonLibrary.Operations;
namespace MasterServer.Operations
{
    public class HandleResponse
    {
        public ClientPeer ClientPeer { get; }
        public OperationCode OperationCode { get; }
        public byte[]? ResponseData { get; set; } = null;

        public HandleResponse(ClientPeer clientPeer, OperationCode operationCode)
        {
            ClientPeer = clientPeer;
            OperationCode = operationCode;
        }
    }
}
