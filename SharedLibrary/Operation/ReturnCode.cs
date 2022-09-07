namespace SharedLibrary.Operation
{
    public enum ReturnCode : byte
    {
        Success,
        Failed,

        InvalidOperation,

        AlreadyAuth,
        AuthTokenError,
        NoAuth,

        NotInLobby,
        GetLobbyFailed,
        AlreadyJoinLobby,

        CreateRooming,
        NoMatchGameServer,
        AlreadyInRoom,
        PasswordError,
        FullRoom,
        NotInRoom,
        RoomNotExisted,
        JoinRooming,
        
    }
}