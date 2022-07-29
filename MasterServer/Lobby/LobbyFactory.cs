namespace MasterServer
{
    public class LobbyFactory
    {
        private static LobbyFactory? instance;
        private List<Lobby> lobbies = new List<Lobby>();

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

        public Lobby? GetLobby(string lobbyID)
        {
            Lobby? lobby = lobbies.FirstOrDefault((a) => a.LobbyID == lobbyID);
            return lobby;
        }

        public Lobby GetOrCreateLobby()
        {
            Lobby? lobby = lobbies.FirstOrDefault((a) =>a.IsFullLobby == false);
            if (lobby == null)
            {
                lobby = new Lobby();
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
