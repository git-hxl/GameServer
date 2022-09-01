using LiteNetLib;

namespace MasterServer
{
    internal class GameServerPeer
    {
        private NetPeer peer;

        public GameServerPeer(NetPeer peer)
        {
            this.peer = peer;
        }
    }
}