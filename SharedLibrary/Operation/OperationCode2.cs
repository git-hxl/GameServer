namespace SharedLibrary.Operation
{
    public enum OperationCode2 : byte
    {
        None,
        Auth,
        JoinLobby,
        LeaveLobby,
        CreateRoom,
        JoinRoom,
        LeaveRoom,
        GetRoomList,
        UpdateRoomProperties,

        RPC,

        RegisterGameServer,
        UpdateGameServer,
    }
}
