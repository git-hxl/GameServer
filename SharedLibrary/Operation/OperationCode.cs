namespace SharedLibrary
{
    public enum OperationCode : ushort
    {
        UpdateGameServerInfo = 0,
        UpdateRoomList,

        Register,
        Login,
        GetRoomList,
        CreateRoom,
        JoinRoom,
        LeaveRoom,

        OtherJoinedRoom,
        OtherLeaveRoom,

        SyncEvent,
    }
}