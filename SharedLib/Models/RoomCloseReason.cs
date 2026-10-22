namespace SharedLib.Models;

public enum RoomCloseReason : byte
{
    Empty = 0,
    ClosedBySystem = 1,
    GameServerOffline = 2
}
