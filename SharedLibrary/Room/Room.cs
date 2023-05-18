using SharedLibrary.Message;
using SharedLibrary.Server;

namespace SharedLibrary.Room
{
    public class Room
    {
        public RoomInfo RoomInfo { get; private set; }
        public List<ServerPeer> ServerPeers { get; private set; }
        public DateTime CreateTime { get; private set; }
        private static object locker { get; } = new object();

        public Room(RoomInfo roomInfo)
        {
            RoomInfo = roomInfo;
            CreateTime = DateTime.Now;
            ServerPeers = new List<ServerPeer>();
        }


        public bool AddPlayer(ServerPeer serverPeer)
        {
            lock (locker)
            {
                if (!ServerPeers.Contains(serverPeer) && ServerPeers.Count < RoomInfo.RoomMaxPlayers)
                {
                    ServerPeers.Add(serverPeer);
                    return true;
                }
                return false;
            }
        }

        public bool RemovePlayer(ServerPeer serverPeer)
        {
            lock (locker)
            {
                if (ServerPeers.Contains(serverPeer))
                {
                    ServerPeers.Remove(serverPeer);
                    return true;
                }

                return false;
            }
        }
    }
}
