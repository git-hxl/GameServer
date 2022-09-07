namespace SharedLibrary.Operation
{
    public enum OperationCode : byte
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
