using LiteNetLib;
using LiteNetLib.Utils;
namespace MasterServer
{
    public class OperationResponse
    {
        public OperationCode OperationCode { get; private set; }
        public ReturnCode ReturnCode { get; private set; }
        public byte[] Data { get; private set; }
        public DeliveryMethod DeliveryMethod { get; private set; }
        public OperationResponse(OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            OperationCode = operationCode;
            ReturnCode = returnCode;
            Data = data;
            DeliveryMethod = deliveryMethod;
        }

        public void SendTo(params NetPeer[] netPeers)
        {
            if (OperationCode.None == OperationCode)
                return;

            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)1);
            netDataWriter.Put((byte)OperationCode);
            netDataWriter.Put((byte)ReturnCode);
            if (Data != null && Data.Length > 0)
            {
                netDataWriter.Put(Data);
            }
            foreach (var peer in netPeers)
            {
                peer.Send(netDataWriter, DeliveryMethod);
            }
        }

        public static OperationResponse CreateResponse(OperationRequest operationRequest, ReturnCode returnCode, byte[] data)
        {
            return new OperationResponse(operationRequest.OperationCode, returnCode, data, operationRequest.DeliveryMethod);
        }

        public static OperationResponse CreateFailedResponse(OperationRequest operationRequest, ReturnCode returnCode)
        {
            return new OperationResponse(operationRequest.OperationCode, returnCode, null, operationRequest.DeliveryMethod);
        }

        public static OperationResponse CreateNoneResponse()
        {
            return new OperationResponse(OperationCode.None, ReturnCode.Success, null, DeliveryMethod.ReliableOrdered);
        }
    }
}