namespace CommonLibrary.Operations
{
    public class RequestBase
    {
        public OperationCode OperationCode;
        public string Token { get; set; }
        public long TimeStamp { get; set; }
    }
}
