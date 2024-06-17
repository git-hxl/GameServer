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

        HotLoad,

        SyncEvent = 1000,
    }
}