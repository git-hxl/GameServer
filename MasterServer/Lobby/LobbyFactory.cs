namespace MasterServer
{
    public class LobbyFactory
    {
        public static LobbyFactory Instance { get; private set; } = new LobbyFactory();
        public List<Lobby> Lobbies { get; private set; } = new List<Lobby> ();

        public Lobby? GetLobby(string lobbyID)
        {
            Lobby? lobby = Lobbies.FirstOrDefault((a) => a.LobbyProperty.LobbyID == lobbyID);
            return lobby;
        }

        public Lobby GetOrCreateLobby()
        {
            Lobby? lobby = Lobbies.FirstOrDefault((a) =>a.IsFullLobby == false);
            if (lobby == null)
            {
                lobby = new Lobby();
                Lobbies.Add(lobby);
            }
            return lobby;
        }

        public bool RemoveLobby(Lobby lobby)
        {
            if (Lobbies.Contains(lobby))
            {
                Lobbies.Remove(lobby);
                return true;
            }
            return false;
        }
    }
}
