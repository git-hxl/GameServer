namespace SharedLib.Protocol
{
    /// <summary>
    /// 消息 ID 常量
    /// </summary>
    public static class MessageIds
    {
        public const ushort JoinLobby = 1;
        public const ushort LeaveLobby = 2;
        public const ushort Chat = 3;
        public const ushort ChatNotify = 4;

        // 房间
        public const ushort CreateRoom = 10;
        public const ushort JoinRoom = 11;
        public const ushort LeaveRoom = 12;
        public const ushort JoinRoomNotify = 13;
        public const ushort LeaveRoomNotify = 14;
        public const ushort RoomList = 15;
        public const ushort RoomOwnerChangedNotify = 16;
        public const ushort RoomClosedNotify = 17;

        // 游戏房间（GameServer 侧）
        public const ushort JoinGame = 30;
        public const ushort LeaveGame = 31;
        public const ushort JoinGameNotify = 32;
        public const ushort LeaveGameNotify = 33;

        // GameServer 内部通信
        public const ushort GameServerRegister = 100;
        public const ushort GameServerUpdate = 101;
        public const ushort CreateGameRoom = 102;
        public const ushort CloseGameRoom = 103;

        // 同步
        public const ushort PositionSync = 40;
        public const ushort AnimationSync = 41;
        public const ushort ObjectSpawn = 42;
        public const ushort ObjectDespawn = 43;
    }
}
