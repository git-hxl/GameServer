namespace CommonLibrary.Core
{ 
    public enum ReturnCode
    {
        OnRegister,
        OnRegisterFailed,

        OnLogin,
        OnLoginFailed,

        OnJoinLobby,
        OnJoinLobbyFailed,

        OnLeaveLobby,
        OnLeaveLobbyFailed,

        OnCreateRoom,
        OnCreateRoomFailed,
        OnJoinRoom,
        OnJoinRoomFailed,
        OnLeaveRoom,
        OnLeaveRoomFailed,

        OnRoomListUpdate,
        OnRoomCustomPropertyUpdate,

        OnPlayerLeaveRoom,
        OnPlayerJoinRoom,

        OnRpc,

        InvalidRequest,

        OnCreateGame,
        OnRemoveGame,
        OnJoinGame,
        OnLeaveGame,
    }
}