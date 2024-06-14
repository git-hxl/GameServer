
using Serilog;
using SharedLibrary;
using SharedLibrary.Utils;

namespace GameServer
{
    public class RoomManager
    {
        public static RoomManager Instance { get; private set; } = new RoomManager();

        private Dictionary<string, Room> _rooms = new Dictionary<string, Room>();

        private long lastUpdateTimeStamp;

        public RoomManager()
        {
            lastUpdateTimeStamp = DateTimeUtil.TimeStamp;
        }

        public List<Room> GetRooms()
        {
            return _rooms.Values.ToList();
        }

        public bool CreateRoom(RoomInfo roomInfo)
        {
            lock (Instance)
            {
                if (_rooms.ContainsKey(roomInfo.RoomID))
                {
                    Log.Error("房间已存在！！！");
                    return false;
                }
                else
                {
                    Room room = new Room(roomInfo);

                    _rooms[roomInfo.RoomID] = room;

                    return true;
                }
            }
        }

        public Room? GetRoom(string roomID)
        {
            if (_rooms.ContainsKey(roomID))
            {
                return (_rooms[roomID]);
            }
            else
            {
                return null;
            }
        }

        public Room? GetRoomByClientPeer(BasePeer basePeer)
        {
            foreach (var item in _rooms)
            {
                if (item.Value.ClientPeers.Contains(basePeer))
                {
                    return item.Value;
                }
            }

            return null;
        }


        public void RemoveOfflinePlayer(ClientPeer peer)
        {
            foreach (var item in _rooms)
            {
                if (item.Value.ClientPeers.Contains(peer))
                {
                    item.Value.RemoveClient(peer);
                }
            }
        }


        public void Update()
        {
            long deltaTime = DateTimeUtil.TimeStamp - lastUpdateTimeStamp;
            lastUpdateTimeStamp = DateTimeUtil.TimeStamp;

            var rooms = _rooms.Values.ToList();

            foreach (var room in rooms)
            {
                if (room.IsActive == false)
                {
                    _rooms.Remove(room.RoomInfo.RoomID);

                    Log.Information("清理不活跃的房间{0}", room.RoomInfo.RoomID);
                }

                else
                {
                    room.Update(deltaTime);
                }
            }
        }
    }
}