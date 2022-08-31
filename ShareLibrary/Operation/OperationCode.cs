namespace ShareLibrary
{
    public enum OperationCode : byte
    {
        Auth = 0,
        JoinLobby,
        LeaveLobby,

        CreateGame,
        JoinGame,
        LeaveGame,

        RegisterGameServer,
    }
}
