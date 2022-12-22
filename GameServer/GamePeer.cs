using LiteNetLib;
using SharedLibrary.Model;
using SharedLibrary.Server;

namespace GameServer
{
    internal class GamePeer : ServerPeer
    {
        public bool IsMaster { get; private set; }
        public GamePeer(NetPeer peer) : base(peer)
        {
        }

        public void OnGameServerRegisterResponse(byte[] data)
        {
            IsMaster = true;
            GameServer.Instance.OnRegisterToMasterSuccess(this);
        }
    }
}
