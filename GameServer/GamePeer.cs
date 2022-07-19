using LiteNetLib;
using Serilog;
using System.Collections;

namespace GameServer
{
    public sealed class GamePeer
    {
        public int UserID { get; set; }

        public NetPeer NetPeer { get; private set; }

        private Game? CurGame;

        public GamePeer(NetPeer netPeer)
        {
            this.NetPeer = netPeer;
        }

        public void OnJoinGame(Game game)
        {
            CurGame = game;
        }

        public void OnExitGame()
        {
            CurGame = null;
        }

        public void OnDisConnected()
        {
            if (CurGame != null)
            {
                CurGame.RemovePeer(this);
            }
        }
    }
}
