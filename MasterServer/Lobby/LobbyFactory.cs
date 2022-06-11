namespace MasterServer.Lobby
{
    internal class LobbyFactory
    {
        private Dictionary<string, Lobby> lobbyDict = new Dictionary<string, Lobby>();
        public Lobby GetOrCreateLobby(string lobbyName)
        {
            lobbyDict[lobbyName]
        }
    }
}
