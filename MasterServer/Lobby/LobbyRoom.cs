using System.Collections;

namespace MasterServer.Lobby
{
    public class LobbyRoom
    {
        public string RoomID { get; }
        public string RoomName { get; }
        public bool IsVisible { get; }
        public string Password { get; }
        public int MaxPeers { get; }
        public Hashtable RoomProperties { get; }
        public List<ClientPeer> ClientPeers { get; }

        private Lobby lobby;
        public LobbyRoom(Lobby lobby, ClientPeer clientPeer, string roomName, bool isVisible, string password, int maxPeers, Hashtable roomProperties)
        {
            this.lobby = lobby;
            this.RoomID = Guid.NewGuid().ToString();
            this.RoomName = roomName;
            this.IsVisible = isVisible;
            this.Password = password;
            this.MaxPeers = maxPeers;
            this.RoomProperties = roomProperties;

            ClientPeers = new List<ClientPeer>();
            ClientPeers.Add(clientPeer);
        }

        public bool AddClientPeer(ClientPeer clientPeer)
        {
            if (!ClientPeers.Contains(clientPeer) && ClientPeers.Count < MaxPeers)
            {
                ClientPeers.Add(clientPeer);
                return true;
            }
            return false;
        }

        public void RemoveClientPeer(ClientPeer clientPeer)
        {
            if (ClientPeers.Contains(clientPeer))
            {
                ClientPeers.Remove(clientPeer);
                if (ClientPeers.Count <=0)
                {
                    lobby.RemoveRoom(this);
                }
            }
        }
    }
}