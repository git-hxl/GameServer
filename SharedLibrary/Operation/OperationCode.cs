namespace SharedLibrary
{
    public enum OperationCode : ushort
    {
        UpdateGameServerInfo = 0,
        UpdateRoomList,
        CloseRoom,

        Register,
        Login,
        GetRoomList,
        CreateRoom,
        JoinRoom,
        LeaveRoom,

        OtherJoinedRoom,
        OtherLeaveRoom,

        SyncEvent = 1000,
    }
}