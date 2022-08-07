namespace CommonLibrary.Core
{
    public enum OperationCode
    {
        Register,
        Login,
        JoinLobby,
        LeaveLobby,
        CreateRoom,
        JoinRoom,
        LeaveRoom,
        GetRoomList,
        UpdateRoomProperty,
       
        RegisterGameServer,

        Rpc,
        JoinGame,
        LeaveGame,

        CreateGame,
        RemoveGame,

    }
}