namespace MasterServer.Lobby
{
    internal class LobbyFactory
    {
        private List<Lobby> lobbies = new List<Lobby>();

        public Lobby GetOrCreateLobby(string lobbyName)
        {
            Lobby? lobby = lobbies.FirstOrDefault((a) => a.LobbyName == lobbyName);
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
