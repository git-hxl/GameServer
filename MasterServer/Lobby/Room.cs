using MessagePack;
using System.Collections;

namespace MasterServer
{
    [MessagePackObject]
    public class RoomProperty
    {
        [Key(0)]
        public string RoomID = "";
        [Key(1)]
        public string RoomName = "";
        [Key(2)]
        public bool IsVisible;
        [Key(3)]
        public bool NeedPassword;
        [IgnoreMember]
        public string Password = "";
        [Key(4)]
        public int MaxPeers;
        [Key(5)]
        public Hashtable CustomProperties = new Hashtable();
    }
    public class Room
    {
        public RoomProperty RoomProperty { get; }
        public List<MasterPeer> ClientPeers { get; }
        public Lobby Lobby { get; }
        public Room(Lobby lobby, MasterPeer clientPeer, string roomName, bool isVisible, bool needPassword, string password, int maxPeers, Hashtable customProperties)
        {
            RoomProperty = new RoomProperty();
            RoomProperty.RoomID = Guid.NewGuid().ToString();
            RoomProperty.RoomName = roomName;
            RoomProperty.IsVisible = isVisible;
            RoomProperty.NeedPassword = needPassword;
            RoomProperty.Password = password;
            RoomProperty.MaxPeers = maxPeers;
            RoomProperty.CustomProperties = customProperties;
            ClientPeers = new List<MasterPeer>();
            Lobby = lobby;
            AddClientPeer(clientPeer);
        }

        public bool AddClientPeer(MasterPeer clientPeer)
        {
            if (!ClientPeers.Contains(clientPeer) && ClientPeers.Count <RoomProperty.MaxPeers)
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