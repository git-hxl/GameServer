using CommonLibrary.MessagePack.Operation;

namespace MasterServer.Operations
{
    public class HandleResponse
    {
        public MasterPeer ClientPeer { get; }
        public OperationCode OperationCode { get; }
        public byte[]? ResponseData { get; set; } = null;

        public HandleResponse(MasterPeer clientPeer, OperationCode operationCode)
        {
            ClientPeer = clientPeer;
            OperationCode = operationCode;
        }
    }
}
