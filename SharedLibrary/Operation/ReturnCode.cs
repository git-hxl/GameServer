namespace SharedLibrary.Operation
{
    public enum ReturnCode : byte
    {
        Success = 0,

        RegisterFailed,
        RegisterFailedByAccountIsExisted,
        LoginFailed,

        ConnectFailed,

        JoinRoomFailed,
        CreateRoomFailed,
        JoinRoomFailedByIsInRoom,
        JoinRoomFailedByPassword,
        JoinRoomFailedByIsNotExistedRoomID,

        LeaveRoomFailed,

    }
}
