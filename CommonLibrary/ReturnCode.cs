namespace CommonLibrary.Operations
{
    public enum ReturnCode
    {
        Success = 0,
        RegisterFailed,
        LoginFailed,

        JoinLobbyFailed,
        LeaveLobbyFailed,

        CreateRommFailed,
        JoinRoomFailed,
        LeaveRoomFailed,

        RPCFailed,

        InvalidRequest,
    }

}
