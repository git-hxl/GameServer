using MasterServer;

namespace GameServer
{
    internal class RoomState
    {
        public string RoomID { get; private set; }

        public CreateRoomRequest RoomInfo { get; private set; }

        public List<string> Players { get; private set; } = new List<string>();
        public string RoomOwner { get; private set; }

        public RoomState(string roomID, CreateRoomRequest roomInfo)
        {
            RoomID = roomID;
            RoomInfo = roomInfo;
            RoomOwner = roomInfo.UserID;
        }

        public void AddPlayer(string userID)
        {
            lock (this)
            {
                Players.Add(userID);
            }
        }

        public void RemovePlayer(string userID)
        {
            lock (this)
            {
                Players.Remove(userID);
            }
        }
    }
}
