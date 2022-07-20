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
        public bool JoinGame(Game game, int userID)
        {
            if (CurGame == null)
            {
                this.UserID = userID;
                CurGame = game;
                CurGame.AddPeer(this);
                return true;
            }
            return false;
        }

        public bool ExitGame()
        {
            if (CurGame != null)
            {
                CurGame.RemovePeer(this);
                CurGame = null;
                return true;
            }
            return false;
        }

        public void OnDisConnected()
        {
            if (CurGame != null)
            {
                CurGame.RemovePeer(this);
                CurGame = null;
            }
        }
    }
}
