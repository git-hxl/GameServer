namespace MasterServer
{
    internal class PlayerCache
    {
        public static PlayerCache Instance { get; private set; } = new PlayerCache();

        private Dictionary<int, MasterClientPeer> players = new Dictionary<int, MasterClientPeer>();

        public int Count { get { { return players.Count; } } }

        public bool ContainsKey(int id)
        {
            lock (this)
            {
                return players.ContainsKey(id);
            }
        }

        public MasterClientPeer? GetPlayer(int id)
        {
            lock (this)
            {
                if (players.ContainsKey(id))
                    return players[id];

                else
                    return null;
            }
        }

        public void AddPlayer(int id, MasterClientPeer masterClientPeer)
        {
            lock (this)
            {
                players[id] = masterClientPeer;
            }
        }

        public void RemovePlayer(int id)
        {
            lock (this)
            {
                if (players.ContainsKey(id))
                    players.Remove(id);
            }
        }
    }
}
