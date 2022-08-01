namespace CommonLibrary.Core
{
    public enum OperationCode
    {
        Register,
        Login,
        JoinLobby,
        LevelLobby,
        CreateRoom,
        JoinRoom,
        LeaveRoom,
        GetRoomList,
       
        RegisterGameServer,

        Rpc,
        JoinGame,
        LeaveGame,

        CreateGame,
        RemoveGame,

        OnPlayerJoinRoom,
        OnPlayerLeaveRoom,
        OnRoomListUpdate,

        OnPlayerJoinGame,
        OnPlayerLeaveGame,
    }
}