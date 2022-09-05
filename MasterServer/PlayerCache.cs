namespace MasterServer
{
    internal class PlayerCache
    {
        public static PlayerCache Instance { get; private set; } = new PlayerCache();

        private Dictionary<string, MasterClientPeer> players = new Dictionary<string, MasterClientPeer>();

        public int Count { get { { return players.Count; } } }

        public bool ContainsKey(string userID)
        {
            lock (this)
            {
                return players.ContainsKey(userID);
            }
        }

        public MasterClientPeer? GetPlayer(string userID)
        {
            lock (this)
            {
                if (players.ContainsKey(userID))
                    return players[userID];

                else
                    return null;
            }
        }

        public void AddPlayer(string userID, MasterClientPeer masterClientPeer)
        {
            lock (this)
            {
                players[userID] = masterClientPeer;
            }
        }

        public void RemovePlayer(string userID)
        {
            lock (this)
            {
                if (players.ContainsKey(userID))
                    players.Remove(userID);
            }
        }
    }
}
