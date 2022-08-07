using MessagePack;
using Serilog;
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
        public List<MasterPeer> MasterPeers { get; }
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
            MasterPeers = new List<MasterPeer>();
            Lobby = lobby;
        }

        public bool AddClientPeer(MasterPeer masterPeer, string password)
        {
            if (!RoomProperty.NeedPassword || (RoomProperty.NeedPassword && RoomProperty.Password == password))
            {
                if (!MasterPeers.Contains(masterPeer) && MasterPeers.Count < RoomProperty.MaxPeers)
                {
                    MasterPeers.Add(masterPeer);
                    Log.Information("{0} Join Room: {1}", masterPeer.NetPeer.EndPoint.ToString(), RoomProperty.RoomID);
                    return true;
                }
            }
            return false;
        }

        public void RemoveClientPeer(MasterPeer masterPeer)
        {
            if (MasterPeers.Contains(masterPeer))
            {
                MasterPeers.Remove(masterPeer);

                if (MasterPeers.Count <= 0)
                {
                    Lobby.RemoveRoom(this);
                }

                Log.Information("{0} Leave Room: {1}", masterPeer.NetPeer.EndPoint.ToString(), RoomProperty.RoomID);
            }
        }
    }
}