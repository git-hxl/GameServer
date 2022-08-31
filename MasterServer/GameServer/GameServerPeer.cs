using LiteNetLib;
namespace MasterServer.GameServer
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