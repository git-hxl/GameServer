using LiteNetLib;
using LiteNetLib.Utils;
using SharedLibrary.Message;
using SharedLibrary.Operation;
namespace SharedLibrary.Server
{
    public class ServerPeer
    {
        public NetPeer NetPeer { get; private set; }

        public UserInfo UserInfo { get; set; }

        public ServerPeer(NetPeer netPeer)
        {
            NetPeer = netPeer;
        }


        public void SendRequestToServer(ServerOperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            SendRequest((byte)operationCode, data, 0, deliveryMethod);
        }

        public void SendResponseToServer(ServerOperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            SendResponse((byte)operationCode, (byte)returnCode, data, 1, deliveryMethod);
        }

        public void SendRequestToClient(OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            SendRequest((byte)operationCode, data, 2, deliveryMethod);
        }

        public void SendResponseToClient(OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            SendResponse((byte)operationCode, (byte)returnCode, data, 3, deliveryMethod);
        }

        public void SendRequest(byte operationCode, byte[] data, byte channel, DeliveryMethod deliveryMethod)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put(operationCode);
            if (data != null)
                netDataWriter.Put(data);
            NetPeer.Send(netDataWriter, channel, deliveryMethod);
        }

        public void SendResponse(byte operationCode, byte returnCode, byte[] data, byte channel, DeliveryMethod deliveryMethod)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put(operationCode);
            netDataWriter.Put(returnCode);
            if (data != null)
                netDataWriter.Put(data);
            NetPeer.Send(netDataWriter, channel, deliveryMethod);
        }
    }
}
