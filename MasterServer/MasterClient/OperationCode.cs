﻿namespace MasterServer.MasterClient
{
    public enum OperationCode : byte
    {
        Auth,
        JoinLobby,
        LeaveLobby,
        CreateRoom,
        JoinRoom,
        LeaveRoom,
        GetRoomList,
        UpdateRoomProperties
    }
}
