
using LiteNetLib;
using LiteNetLib.Utils;

namespace ShareLibrary
{
    public class OperationResponse
    {
        public OperationCode OperationCode { get; private set; }
        public DeliveryMethod DeliveryMethod { get; private set; }
        public ReturnCode ReturnCode { get; private set; }
        public byte[] Data { get; private set; }

        public OperationResponse(OperationCode operationCode, DeliveryMethod deliveryMethod, ReturnCode returnCode, byte[] data)
        {
            this.OperationCode = operationCode;
            this.DeliveryMethod = deliveryMethod;
            this.ReturnCode = returnCode;
            this.Data = data;
        }

        public void SendTo(NetPeer netPeer)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationCode);
            netDataWriter.Put((short)ReturnCode);
            if (Data != null && Data.Length > 0)
            {
                netDataWriter.Put(Data);
            }
            netPeer.Send(netDataWriter, DeliveryMethod);
        }

        public static OperationResponse CreateDefaultResponse(OperationRequest operationRequest, byte[] data)
        {
            return new OperationResponse(operationRequest.OperationCode, operationRequest.DeliveryMethod, ReturnCode.Success, data);
        }

        public static OperationResponse CreateFailedResponse(OperationRequest operationRequest, ReturnCode returnCode)
        {
            return new OperationResponse(operationRequest.OperationCode, operationRequest.DeliveryMethod, returnCode, null);
        }
    }
}
