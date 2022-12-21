namespace SharedLibrary.Operation
{
    public enum OperationCode : byte
    {
        //Server
        GameServerRegister = 0,
        UpdateServerState,

        //Client
        Register,
        Login,
        Logout,

        JoinRoom,
        LeaveRoom,
        CreateRoom,

        GetRoomList,
    }
}