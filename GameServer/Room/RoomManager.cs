
using LiteNetLib;
using Serilog;
using SharedLibrary;
using SharedLibrary.Utils;

namespace GameServer
{
    public class RoomManager
    {
        public static RoomManager Instance { get; private set; } = new RoomManager();

        private Dictionary<string, IRoom> _rooms = new Dictionary<string, IRoom>();

        private long lastUpdateTimeStamp;

        public RoomManager()
        {
            lastUpdateTimeStamp = DateTimeUtil.TimeStamp;
        }

        public List<IRoom> GetRooms()
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
                    IRoom room = RoomFactory.CreatRoom(roomInfo.RoomType, roomInfo.RoomName);

                    if (room == null)
                    {
                        Log.Error("房间不存在：{0}，{1}", roomInfo.RoomType, roomInfo.RoomName);
                        return false;
                    }

                    room.OnCreated(roomInfo);
                    _rooms[roomInfo.RoomID] = room;

                    return true;
                }
            }
        }

        public IRoom? GetRoom(string roomID)
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

        public IRoom? GetRoomByClientPeer(BasePeer basePeer)
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
                    item.Value.OnPlayerLeave(peer);
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
                    string roomID = room.RoomInfo.RoomID;
                    _rooms.Remove(roomID);

                    Log.Information("清理不活跃的房间{0}", roomID);
                }

                else
                {
                    room.OnUpdate(deltaTime);
                }
            }
        }
    }
}