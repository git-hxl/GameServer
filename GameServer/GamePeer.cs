using LiteNetLib;
using Serilog;
using System.Collections;

namespace GameServer
{
    public sealed class GamePeer
    {
        public int UserID { get; private set; }

        public NetPeer NetPeer { get; private set; }

        public Game? CurGame { get; private set; }

        public GamePeer(NetPeer netPeer ,int userID)
        {
            NetPeer = netPeer;
            UserID = userID;
        }
        public void OnJoinGame(Game game)
        {
            CurGame = game;
        }

        public void OnLeaveGame()
        {
            CurGame = null;
        }

        public void OnDisConnected()
        {
            CurGame?.RemoveClientPeer(this);
            OnLeaveGame();
        }
    }
}
