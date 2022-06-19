using System.Collections;

namespace MasterServer.Lobby
{
    internal class LobbyRoom
    {
        public string LobbyName { get; }
        public byte MaxPeers { get; }
        public Hashtable roomProperties { get; }
        public LobbyRoom(string lobbyName, Hashtable roomProperties)
        {
            this.LobbyName = lobbyName;
            this.roomProperties = roomProperties;
        }
    }
}