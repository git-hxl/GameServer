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

        public bool AddPeer(NetPeer netPeer, int userID)
        {
            GamePeer? gamePeer = GameApplication.Instance.GetGamePeer(netPeer);
            if (gamePeer != null && !GamePeers.Contains(gamePeer))
            {
                gamePeer.UserID = userID;
                GamePeers.Add(gamePeer);
                gamePeer.OnJoinGame(this);
                return true;
            }
            return false;
        }

        public bool RemovePeer(GamePeer gamePeer)
        {
            if (gamePeer != null && GamePeers.Contains(gamePeer))
            {
                gamePeer.OnExitGame();
                GamePeers.Remove(gamePeer);
                return true;
            }
            return false;
        }
    }
}
