using LiteNetLib;
namespace CoreLibrary
{
    public abstract class OperationHandlerBase
    {
        protected abstract OperationResponse OnOperationRequest(NetPeer peer, OperationRequest operationRequest);
    }
}
