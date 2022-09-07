using GameServer.Client;

namespace GameServer
{
    internal class PlayerManager
    {
        public static PlayerManager Instance { get; private set; } = new PlayerManager();

        public Dictionary<int, ClientPeer> ClientPeers { get; private set; } = new Dictionary<int, ClientPeer>();

        private PlayerManager() { }

        public void AddClientPeer(int id, ClientPeer clientPeer)
        {
            ClientPeers[id] = clientPeer;
        }

        public ClientPeer? GetClientPeer(int id)
        {
            if (ClientPeers.ContainsKey(id))
            {
                return ClientPeers[id];
            }

            return null;
        }

        public ClientPeer? RemoveClientPeer(int id)
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
