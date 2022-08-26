namespace CoreLibrary
{
    public class OperationResponse
    {
        public OperationCode OperationCode { get; private set; }

        public ReturnCode ReturnCode { get; private set; }

        public byte[] data { get; private set; }

        public OperationResponse(OperationCode operationCode,ReturnCode returnCode, byte[] data)
        {
            this.OperationCode = operationCode;
            this.ReturnCode = returnCode;
            this.data = data;
        }
    }
}
