namespace ShareLibrary
{
    public enum ReturnCode : byte
    {
        Success,

        InvalidOperation,
        AuthTokenError,
        AlreadyAuth,

        NoAuth,

        AlreadyJoinLobby,
        GetLobbyFailed,
        NotInLobby,


        RegisterGameServerFailed
    }
}
