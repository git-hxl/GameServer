namespace MasterServer
{
    public enum ReturnCode : byte
    {
        Success,
        Failed,

        InvalidOperation,

        AlreadyAuth,
        AuthTokenError,

        NotInLobby,
        GetLobbyFailed,
        AlreadyJoinLobby,

        CreateRooming,
        NoMatchGameServer,
        AlreadyInRoom,
    }
}