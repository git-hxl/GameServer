
using System.Collections.Concurrent;

namespace MasterServer
{
    internal class LobbyFactory
    {
        private AppLobby defaultLobby;
        private string defaultLobbyKey = "Default";
        private ConcurrentDictionary<string, AppLobby> lobbyDict = new ConcurrentDictionary<string, AppLobby>();

        public int LobbyCount { get { return lobbyDict.Count; } }

        public LobbyFactory()
        {
            this.defaultLobby = new AppLobby(defaultLobbyKey);
            this.lobbyDict[defaultLobbyKey] = defaultLobby;
        }

        public bool GetOrCreateLobby(string lobbyName, out AppLobby lobby)
        {
            if (string.IsNullOrEmpty(lobbyName))
            {
                lobbyName = defaultLobbyKey;
            }
            if (lobbyDict.ContainsKey(lobbyName))
            {
                lobby = lobbyDict[lobbyName];
            }
            else
            {
                lobby = new AppLobby(lobbyName);
                lobbyDict[lobbyName] = lobby;
            }
            return true;
        }
    }
}