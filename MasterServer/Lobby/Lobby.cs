using LiteNetLib;

namespace MasterServer.Lobby
{
    internal class Lobby
    {
        public readonly string LobbyName;
        public readonly int MaxPeers;
        private List<NetPeer> peers = new List<NetPeer>();

        public Lobby(string name)
        {
            this.LobbyName = name;
            MaxPeers = -1;
        }

        public void OnJoinLobby(NetPeer netPeer)
        {
            if (!peers.Contains(netPeer))
            {
                peers.Add(netPeer);

                Console.WriteLine("{0} join lobby: {1}", netPeer.EndPoint.ToString(), LobbyName);
            }
        }

        public void OnLeaveLobby(NetPeer netPeer)
        {
            if (peers.Contains(netPeer))
            {
                peers.Remove(netPeer);

                Console.WriteLine("{0} Leave lobby: {1}", netPeer.EndPoint.ToString(), LobbyName);
            }
        }
    }
}
