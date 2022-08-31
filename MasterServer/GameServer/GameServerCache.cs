namespace MasterServer.GameServer
{
    internal class GameServerCache
    {
        public static GameServerCache Instance { get; private set; } = new GameServerCache();

        private Dictionary<int, GameServerPeer> gameServers = new Dictionary<int, GameServerPeer>();

        public int Count { get { lock (this) { return gameServers.Count; } } }

        public bool ContainsKey(int id)
        {
            lock (this)
            {
                return gameServers.ContainsKey(id);
            }
        }

        public GameServerPeer? GetServer(int id)
        {
            lock (this)
            {
                if (gameServers.ContainsKey(id))
                    return gameServers[id];

                else
                    return null;
            }
        }

        public void RegisterGameServer(int id, GameServerPeer gameServerPeer)
        {
            lock (this)
            {
                gameServers[id] = gameServerPeer;
            }
        }

        public void RemoveGameServer(int id)
        {
            lock (this)
            {
                if (gameServers.ContainsKey(id))
                    gameServers.Remove(id);
            }
        }
    }
}
