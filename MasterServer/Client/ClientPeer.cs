
using LiteNetLib;
using SharedLibrary;

namespace MasterServer
{
    public class ClientPeer : BasePeer
    {
        private OperationHandler _operationHandler;
        public ClientPeer(NetPeer peer) : base(peer)
        {
            _operationHandler = new OperationHandler();
        }

        public override void OnRequest(OperationCode operationCode, byte[] data,DeliveryMethod deliveryMethod)
        {
            _operationHandler.OnRequest(this, operationCode, data, deliveryMethod);
        }

        public override void OnResponse(OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            _operationHandler.OnRequest(this, operationCode, data, deliveryMethod);
        }
    }
}