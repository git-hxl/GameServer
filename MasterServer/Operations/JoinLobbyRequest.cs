﻿using MessagePack;
namespace MasterServer
{
    [MessagePackObject]
    public class JoinLobbyRequest
    {
        [Key(0)]
        public string UserID;
        [Key(1)]
        public string LobbyName;
    }

    [MessagePackObject]
    public class JoinLobbyResponse
    {
        [Key(0)]
        public string LobbyName;
    }
}
