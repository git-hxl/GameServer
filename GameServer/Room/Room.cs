using GameServer.Client;

namespace GameServer.Room
{
    public class Room
    {
        public RoomInfo RoomInfo { get; set; }

        public List<ClientPeer> ClientPeers { get; private set; } = new List<ClientPeer>();

        public Room(RoomInfo roomInfo)
        {
            RoomInfo = roomInfo;
        }

        public ClientPeer? GetClientPeer(string userID)
        {
            lock (this)
            {
                return ClientPeers.FirstOrDefault(p => p.PeerInfo.UserID == userID);
            }
        }

        public void AddClientPeer(ClientPeer clientPeer)
        {
            lock (this)
            {
                ClientPeers.Add(clientPeer);
                RoomInfo.CurPeers.Add(clientPeer.PeerInfo);
            }
        }


        public void RemoveClientPeer(ClientPeer clientPeer)
        {
            lock (this)
            {
                if (ClientPeers.Contains(clientPeer))
                {
                    ClientPeers.Remove(clientPeer);
                    RoomInfo.CurPeers.Remove(clientPeer.PeerInfo);

                    //更换房主
                    if (RoomInfo.OwnerID == clientPeer.PeerInfo.UserID)
                    {
                        if (ClientPeers.Count > 0)
                        {
                            RoomInfo.OwnerID = ClientPeers[0].PeerInfo.UserID;
                        }
                    }
                }

                if (ClientPeers.Count <= 0)
                {
                    RoomCache.Instance.RemoveRoom(RoomInfo.RoomID);
                }
            }
        }
    }
}
