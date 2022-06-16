using System.Collections;

namespace MasterServer.Room
{
    internal class Room
    {
        public string LobbyName { get; }
        public Hashtable roomProperties { get; }
        public Room(string lobbyName,Hashtable roomProperties)
        {
            this.LobbyName = lobbyName;
            this.roomProperties = roomProperties;
        }
    }
}
