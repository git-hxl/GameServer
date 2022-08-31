using Serilog;
using ShareLibrary;
namespace MasterServer
{
    internal class AppLobby
    {
        private List<MasterClientPeer> masterClientPeers = new List<MasterClientPeer>();

        public string LobbyName { get; private set; }

        public AppLobby(string lobbyName)
        {
            LobbyName = lobbyName;
        }

        public ReturnCode JoinLobby(MasterClientPeer peer)
        {
            lock (masterClientPeers)
            {
                if (masterClientPeers.Contains(peer))
                {
                    return ReturnCode.AlreadyJoinLobby;
                }
                masterClientPeers.Add(peer);

                OnAddPeer(peer);
                return ReturnCode.Success;
            }
        }

        public ReturnCode LeaveLobby(MasterClientPeer peer)
        {
            lock (masterClientPeers)
            {
                if(!masterClientPeers.Contains(peer))
                {
                    return ReturnCode.NotInLobby;
                }
                masterClientPeers.Remove(peer);

                OnRemovePeer(peer);
                return ReturnCode.Success;
            }
        }

        protected virtual void OnAddPeer(MasterClientPeer peer)
        {
            Log.Information("peer added to lobby: s:'{0}',p:'{1}',u:'{2}',t:'{3}'", LobbyName, peer, peer.UserID, masterClientPeers.Count);
        }

        protected virtual void OnRemovePeer(MasterClientPeer peer)
        {
            Log.Information("peer removed from lobby: s:'{0}',p:'{1}',u:'{2}',t:'{3}'", LobbyName, peer, peer.UserID, masterClientPeers.Count);
        }

    }
}
