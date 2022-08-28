
using LiteNetLib;

namespace ShareLibrary
{
    public class OperationRequest
    {
        public OperationCode OperationCode { get; private set; }
        public DeliveryMethod DeliveryMethod { get; private set; }
        public byte[] Data { get; private set; }
        public OperationRequest(OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            this.OperationCode = operationCode;
            this.DeliveryMethod = deliveryMethod;
            this.Data = data;
        }
    }
}
