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
        Rpc,
        JoinGame,
        LeaveGame,

        MasterCreateGame,
        MasterRemoveGame,
    }
}