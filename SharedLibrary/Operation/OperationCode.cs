namespace SharedLibrary.Operation
{
    public enum OperationCode : byte
    {
        //Client
        Register = 100,
        Login,

        GetRoomList,
        CreateRoom,
        JoinRoom,
        LeaveRoom,
        ReplaceRoomOwner,
    }
}
