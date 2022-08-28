
using System.Collections.Concurrent;

namespace MasterServer
{
    internal class PlayerCache
    {
        private ConcurrentDictionary<int, MasterPeer> masterPeers = new ConcurrentDictionary<int, MasterPeer>();

        public int Count { get { return masterPeers.Count; } }
        

        public void AddPeer(int peerID, MasterPeer masterPeer)
        {
            masterPeers[peerID] = masterPeer;
        }

        public void RemovePeer(int peerID)
        {
            if (masterPeers.ContainsKey(peerID))
            {
                MasterPeer masterPeer;
                masterPeers.TryRemove(peerID, out masterPeer);
            }
        }

        public MasterPeer GetPeer(int peerID)
        {
            if (masterPeers.ContainsKey(peerID))
                return masterPeers[peerID];
            return null;
        }
    }
}
