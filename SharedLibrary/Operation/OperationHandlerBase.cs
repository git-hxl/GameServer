using LiteNetLib;
using SharedLibrary.Server;
namespace SharedLibrary.Operation
{
    public abstract class OperationHandlerBase
    {
        public abstract void OnClientRequest(OperationCode operationCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod);

        public abstract void OnClientResponse(OperationCode operationCode, ReturnCode returnCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod);

        public abstract void OnServerRequest(ServerOperationCode operationCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod);

        public abstract void OnServerResponse(ServerOperationCode operationCode, ReturnCode returnCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod);
    }
}
