
namespace CoreLibrary
{
    public enum OperationCode : byte
    {
        Authenticate,
        JoinLobby,
        LeaveLobby,
        CreateGame,
        JoinGame,
        JoinRandomGame,

        Leave,
        RaiseEvent,
        SetProperties,
        GetProperties,

        FindFriends,
        LobbyStats,
        Rpc,
        GetGameList,
    }
}
