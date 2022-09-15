namespace GameServer.Client
{
    internal class PeerManager
    {
        public static PeerManager Instance { get; private set; } = new PeerManager();

        public Dictionary<int, ClientPeer> ClientPeers { get; private set; } = new Dictionary<int, ClientPeer>();

        private PeerManager() { }

        public void AddClientPeer(int id, ClientPeer clientPeer)
        {
            lock (this)
            {
                ClientPeers[id] = clientPeer;
            }
        }

        public ClientPeer? GetClientPeer(int id)
        {
            lock (this)
            {
                if (ClientPeers.ContainsKey(id))
                {
                    return ClientPeers[id];
                }

                return null;
            }
        }

        public ClientPeer? RemoveClientPeer(int id)
        {
            lock (this)
            {
                ClientPeer? clientPeer = null;
                if (ClientPeers.ContainsKey(id))
                {
                    ClientPeers.Remove(id, out clientPeer);
                }

                return clientPeer;
            }
        }
    }
}
