namespace SharedLib.Protocol
{

    public enum ReturnCode : byte
    {
        Success = 0,
        Error = 1,
        RoomNotFound = 2,
        AlreadyInRoom = 3,
        NoGameServerAvailable = 4,
        NotInLobby = 5,
        NotInRoom = 6,
        NotRoomOwner = 7,
        NotAllReady = 8,
        DeserializeFailed = 9,
    }
}
