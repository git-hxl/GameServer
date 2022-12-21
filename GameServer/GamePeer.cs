using LiteNetLib;
using SharedLibrary.Server;

namespace GameServer
{
    internal class GamePeer : ServerPeer
    {
        public GamePeer(NetPeer peer) : base(peer)
        {
        }
    }
}
