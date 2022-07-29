using System.Collections;

namespace MasterServer
{
    public class Room
    {
        public string RoomID { get; }
        public string RoomName { get; }
        public bool IsVisible { get; }
        public bool NeedPassword { get; }
        public string Password { get; }
        public int MaxPeers { get; }
        public Hashtable RoomProperties { get; }
        public List<MasterPeer> ClientPeers { get; }
        public Lobby Lobby { get; }
        public Room(Lobby lobby, MasterPeer clientPeer, string roomName, bool isVisible, bool needPassword, string password, int maxPeers, Hashtable roomProperties)
        {
            Lobby = lobby;
            RoomID = Guid.NewGuid().ToString();
            RoomName = roomName;
            IsVisible = isVisible;
            NeedPassword = needPassword;
            Password = password;
            MaxPeers = maxPeers;
            RoomProperties = roomProperties;
            ClientPeers = new List<MasterPeer>();
            AddClientPeer(clientPeer);
        }

        public bool AddClientPeer(MasterPeer clientPeer)
        {
            if (!ClientPeers.Contains(clientPeer) && ClientPeers.Count < MaxPeers)
            {
                ClientPeers.Add(clientPeer);
                return true;
            }
            return false;
        }

        public void RemoveClientPeer(MasterPeer clientPeer)
        {
            if (ClientPeers.Contains(clientPeer))
            {
                ClientPeers.Remove(clientPeer);
                if (ClientPeers.Count <= 0)
                {
                    Lobby.RemoveRoom(this);
                }
            }
        }
    }
}