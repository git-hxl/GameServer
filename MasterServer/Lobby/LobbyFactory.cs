namespace MasterServer.Lobby
{
    public class LobbyFactory
    {
        private static LobbyFactory? instance;
        public static LobbyFactory Instance
        {
            get
            {
                if (instance == null)
                    instance = new LobbyFactory();
                return instance;
            }
            private set { }
        }

        private List<Lobby> lobbies = new List<Lobby>();

        public Lobby? GetLobby(string lobbyName)
        {
            Lobby? lobby = lobbies.FirstOrDefault((a) => a.LobbyName == lobbyName);
            return lobby;
        }

        public Lobby? GetOrCreateLobby(string lobbyName = "Default")
        {
            Lobby? lobby = lobbies.FirstOrDefault((a) => a.LobbyName == lobbyName && a.IsFullLobby == false);
            if (lobby == null)
            {
                lobby = new Lobby(lobbyName);
                lobbies.Add(lobby);
            }
            return lobby;
        }

        public bool RemoveLobby(Lobby lobby)
        {
            if (lobbies.Contains(lobby))
            {
                lobbies.Remove(lobby);
                return true;
            }
            return false;
        }
    }
}
