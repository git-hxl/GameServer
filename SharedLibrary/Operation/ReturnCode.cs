namespace SharedLibrary.Operation
{
    public enum ReturnCode : byte
    {
        Success = 0,

        InvalidRequest,
        
        GameServerRegisterFailed,

        RegisterFailed,
        LoginFailed,

        JoinRoomFailed,
        CreateRoomFailed,
    }
}
