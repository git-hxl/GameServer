
namespace GameServer.Room
{
    public class RoomCache
    {
        public static RoomCache Instance { get; private set; } = new RoomCache();

        public Dictionary<string, Room> Rooms { get; private set; } = new Dictionary<string, Room>();

        private RoomCache() { }

        public Room? GetRoom(string roomID)
        {
            if (Rooms.ContainsKey(roomID))
            {
                return Rooms[roomID];
            }
            return null;
        }

        public void AddRoom(string roomID, Room room)
        {
            Rooms[roomID] = room;
        }

        public void RemoveRoom(string roomID)
        {
            if (Rooms.ContainsKey(roomID))
                Rooms.Remove(roomID);
        }
    }
}
