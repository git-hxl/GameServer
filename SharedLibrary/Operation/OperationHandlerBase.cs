using LiteNetLib;

namespace SharedLibrary.Operation
{
    public abstract class OperationHandlerBase
    {
        public abstract void OnOperationRequest(NetPeer peer, OperationRequest operationRequest);
        public abstract void OnOperationResponse(NetPeer peer, OperationResponse operationResponse);
    }
}