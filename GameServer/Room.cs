
namespace GameServer
{
    internal class Room
    {
        private Dictionary<int, GameClientPeer> players = new Dictionary<int, GameClientPeer>();

        public int Count { get { lock (this) { return players.Count; } } }
        public string RoomID { get; private set; }
        public Room(string id)
        {
            RoomID = id; ;
        }

        public bool ContainsKey(int id)
        {
            lock (this)
            {
                return players.ContainsKey(id);
            }
        }

        public GameClientPeer? GetPlayer(int id)
        {
            lock (this)
            {
                if (players.ContainsKey(id))
                    return players[id];

                else
                    return null;
            }
        }

        public void AddPlayer(int id, GameClientPeer masterClientPeer)
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
