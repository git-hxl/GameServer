using LiteNetLib;
namespace MasterServer
{
    sealed class ClientPeer
    {
        public int UserID { get; }
        public NetPeer NetPeer { get; }

        public ClientPeer(int userID, NetPeer netPeer)
        {
            this.UserID = userID;
            this.NetPeer = netPeer;
        }
    }
}
