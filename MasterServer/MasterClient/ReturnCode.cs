namespace MasterServer.MasterClient
{
    public enum ReturnCode : byte
    {
        Success,
        Failed,

        AlreadyAuth,
        AuthTokenError,
    }
}