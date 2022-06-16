using MessagePack;

namespace CommonLibrary.Operations
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class ResponseBase
    {
        public OperationCode OperationCode { get; set; }
        public ReturnCode ReturnCode { get; set; }
    }
}
