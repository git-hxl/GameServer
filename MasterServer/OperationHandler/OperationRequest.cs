using LiteNetLib;
using LiteNetLib.Utils;
namespace MasterServer
{
    public class OperationRequest
    {
        public OperationCode OperationCode { get; private set; }
        public byte[] Data { get; private set; }
        public DeliveryMethod DeliveryMethod { get; private set; }
        public OperationRequest(OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            OperationCode = operationCode;
            DeliveryMethod = deliveryMethod;
            Data = data;
        }

        public void SendTo(params NetPeer[] netPeers)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)0);
            netDataWriter.Put((byte)OperationCode);
            if (Data != null && Data.Length > 0)
            {
                netDataWriter.Put(Data);
            }
            foreach (var peer in netPeers)
            {
                peer.Send(netDataWriter, DeliveryMethod);
            }
        }
    }
}