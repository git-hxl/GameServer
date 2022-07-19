using LiteNetLib;

namespace GameServer
{
    internal class Game
    {
        public string GameID { get; }
        public List<NetPeer> netPeers = new List<NetPeer>();

        public Game(string id)
        {
            this.GameID = id;
        }

        public void Update()
        {

        }
    }
}
