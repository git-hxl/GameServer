using LiteNetLib;

namespace GameServer
{
    public class Game
    {
        public string GameID { get; }
        public List<GamePeer> ClientPeers { get; }
        public Game(string roomID)
        {
            GameID = roomID;
            ClientPeers = new List<GamePeer>();
        }

        public bool AddClientPeer(GamePeer clientPeer)
        {
            if (!ClientPeers.Contains(clientPeer))
            {
                ClientPeers.Add(clientPeer);
                return true;
            }
            return false;
        }

        public void RemoveClientPeer(GamePeer clientPeer)
        {
            if (ClientPeers.Contains(clientPeer))
            {
                ClientPeers.Remove(clientPeer);
            }
        }
    }
}
