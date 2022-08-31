

namespace GameServer
{
    internal class RoomCache
    {
        public static RoomCache Instance { get; private set; } = new RoomCache();

        private Dictionary<string, Room> rooms = new Dictionary<string, Room>();

        public int Count { get { lock (this) { return rooms.Count; } } }

        public bool ContainsKey(string id)
        {
            lock (this)
            {
                return rooms.ContainsKey(id);
            }
        }

        public Room? GetOrCreateRoom(string id)
        {
            lock (this)
            {
                if (rooms.ContainsKey(id))
                    return rooms[id];

                else
                {
                    Room room = new Room(id);
                    rooms[id] = room;
                    return room;
                }
            }
        }


        public void RemoveRoom(string id)
        {
            lock (this)
            {
                if (rooms.ContainsKey(id))
                    rooms.Remove(id);
            }
        }
    }
}
