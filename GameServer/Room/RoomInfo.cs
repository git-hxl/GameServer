using GameServer.Client;
using MessagePack;
using System.Collections;

namespace GameServer.Room
{
    [MessagePackObject]
    public class RoomInfo
    {
        [Key(0)]
        public string RoomID;
        [Key(1)]
        public string OwnerID;
        [Key(2)]
        public string RoomName;
        [Key(3)]
        public int MaxPeers;
        [Key(4)]
        public bool IsVisible;
        [Key(5)]
        public string Password;
        [Key(6)]
        public Hashtable RoomProperties;
        [Key(7)]
        public List<PeerInfo> CurPeers = new List<PeerInfo>();
    }
}
