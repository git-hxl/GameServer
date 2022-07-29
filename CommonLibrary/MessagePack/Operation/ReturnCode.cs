namespace CommonLibrary.MessagePack.Operation
{
    public enum ReturnCode
    {
        Success,

        RegisterFailed,

        LoginFailed,

        JoinLobbyFailed,

        LeaveLobbyFailed,

        CreateRommFailed,

        JoinRoomFailed,
        OnOtherJoinedRoom,

        LeaveRoomFailed,
        OnOtherLeaveRoom,

        RPCFailed,

        InvalidRequest,

    }

}
