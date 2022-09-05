namespace MasterServer
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


        RegisterGameServer,
        UpdateGameServer,
    }
}
