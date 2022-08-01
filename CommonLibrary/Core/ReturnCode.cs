namespace CommonLibrary.Core
{ 
    public enum ReturnCode
    {
        Success,
        InvalidRequest,

        OnRegisterFailed,

        OnLoginFailed,

        OnJoinLobbyFailed,

        OnLeaveLobbyFailed,

        OnCreateRoomFailed,
        OnJoinRoomFailed,
        OnLeaveRoomFailed,

        OnCreateGameFailed,
        OnRemoveGameFailed,
    }
}