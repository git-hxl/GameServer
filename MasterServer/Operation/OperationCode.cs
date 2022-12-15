namespace MasterServer.Operation
{
    public enum OperationCode : Byte
    {
        Register,
        Login,
        Logout,

        JoinRoom,
        LeaveRoom,
        CreateRoom,

        GetRoomList,
    }
}