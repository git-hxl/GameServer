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
            return ClientPeers.FirstOrDefault(p => p.PlayerInfo.UserID == userID);
        }

        public void AddClientPeer(ClientPeer clientPeer)
        {
            ClientPeers.Add(clientPeer);
        }


        public void RemoveClientPeer(ClientPeer clientPeer)
        {
            ClientPeers.Remove(clientPeer);

            if (ClientPeers.Count <= 0)
            {
                RoomCache.Instance.RemoveRoom(RoomInfo.RoomID);
            }
        }
    }
}
