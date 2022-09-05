using MasterServer;

namespace GameServer
{
    internal class RoomCache
    {
        public static RoomCache Instance { get; private set; } = new RoomCache();

        private Dictionary<string, RoomState> rooms = new Dictionary<string, RoomState>();

        public int Count { get { { return rooms.Count; } } }

        public bool ContainsKey(string roomID)
        {
            lock (this)
            {
                return rooms.ContainsKey(roomID);
            }
        }

        public RoomState? GetRoom(string roomID)
        {
            lock (this)
            {
                if (rooms.ContainsKey(roomID))
                    return rooms[roomID];

                else
                    return null;
            }
        }

        public RoomState? AddRoom(string roomID, CreateRoomRequest request)
        {
            lock (this)
            {
                if (!rooms.ContainsKey(roomID))
                {
                    rooms[roomID] = new RoomState(roomID, request);
                    return rooms[roomID];
                }
                return null;
            }
        }

        public void RemoveRoom(string roomID)
        {
            lock (this)
            {
                if (rooms.ContainsKey(roomID))
                    rooms.Remove(roomID);
            }
        }
    }
}
