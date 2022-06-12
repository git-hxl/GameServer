namespace MasterServer.Lobby
{
    internal class LobbyFactory
    {
        private Dictionary<string, Lobby> lobbyDict = new Dictionary<string, Lobby>();
        public Lobby GetOrCreateLobby(string lobbyName)
        {
            Lobby? lobby = null;
            if (!lobbyDict.TryGetValue(lobbyName, out lobby))
            {
                lobby = new Lobby(lobbyName);
                lobbyDict.Add(lobbyName, lobby);
            }
            return lobby;
        }

        //public Lobby GetLobbyByPeerID(int id)
        //{
        //   lobbyDict.
        //}
    }
}
