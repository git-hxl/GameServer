namespace SharedLibrary
{
    public enum ReturnCode : ushort
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
