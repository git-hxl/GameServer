using LiteNetLib;
using LiteNetLib.Utils;
using SharedLibrary.Operation;

namespace SharedLibrary.Server
{
    public abstract class ServerPeer
    {
        public NetPeer Peer { get; private set; }

        public ServerPeer(NetPeer peer)
        {
            Peer = peer;
        }

        public void SendResponse(OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationType.Response);
            netDataWriter.Put((byte)operationCode);
            netDataWriter.Put((byte)returnCode);
            if (data != null)
                netDataWriter.Put(data);
            Peer.Send(netDataWriter, deliveryMethod);
        }

        public void SendRequest(OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationType.Request);
            netDataWriter.Put((byte)operationCode);
            if (data != null)
                netDataWriter.Put(data);
            Peer.Send(netDataWriter, deliveryMethod);
        }
    }
}