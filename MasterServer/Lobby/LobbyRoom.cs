using System.Collections;

namespace MasterServer.Lobby
{
    public class LobbyRoom
    {
        public string RoomID { get; }
        public string RoomName { get; }
        public byte MaxPeers { get; }
        public Hashtable RoomProperties { get; }
        public ClientPeer Owner { get; set; }
        public List<ClientPeer> ClientPeers = new List<ClientPeer>();
        private Lobby lobby;
        public LobbyRoom(Lobby lobby, ClientPeer clientPeer, string roomName, int maxPeers, Hashtable roomProperties)
        {
            this.lobby = lobby;
            this.Owner = clientPeer;
            this.RoomID = Guid.NewGuid().ToString();
            this.RoomName = roomName;
            this.RoomProperties = roomProperties;
            ClientPeers.Add(clientPeer);
        }

        public bool AddClientPeer(ClientPeer clientPeer)
        {
            if (!ClientPeers.Contains(clientPeer))
            {
                ClientPeers.Add(clientPeer);
                return true;
            }
            return false;
        }

        public void RemoveClientPeer(ClientPeer clientPeer)
        {
            if (!ClientPeers.Contains(clientPeer))
            {
                ClientPeers.Remove(clientPeer);
                if (ClientPeers.Count > 0)
                {
                    if (Owner == clientPeer)
                    {
                        Owner = ClientPeers[0];
                    }
                }
                else
                {
                    lobby.RemoveRoom(this);
                }
            }
        }
    }
}