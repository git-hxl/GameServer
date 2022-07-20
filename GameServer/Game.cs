using LiteNetLib;

namespace GameServer
{
    public class Game
    {
        public string GameID { get; private set; }
        public List<GamePeer> GamePeers { get; private set; } = new List<GamePeer>();

        public Game(string id)
        {
            this.GameID = id;
        }

        public bool AddPeer(GamePeer gamePeer)
        {
            if (gamePeer != null && !GamePeers.Contains(gamePeer))
            {
                GamePeers.Add(gamePeer);
                return true;
            }
            return false;
        }

        public bool RemovePeer(GamePeer gamePeer)
        {
            if (gamePeer != null && GamePeers.Contains(gamePeer))
            {
                GamePeers.Remove(gamePeer);
                return true;
            }
            return false;
        }
    }
}
