using LiteNetLib;
using System.Collections;
namespace MasterServer
{
    public class Lobby
    {
        public string LobbyID { get; }
        public ushort MaxPeers { get; }
        public ushort MaxRooms { get; }
        public List<MasterPeer> ClientPeers { get; } = new List<MasterPeer>();
        public List<Room> Rooms { get; } = new List<Room>();
        public Lobby()
        {
            LobbyID = Guid.NewGuid().ToString();
            MaxPeers = 1000;
        }

        public bool IsFullLobby => ClientPeers.Count >= MaxPeers;

        public bool AddClientPeer(MasterPeer clientPeer)
        {
            if (!ClientPeers.Contains(clientPeer))
            {
                ClientPeers.Add(clientPeer);
                Console.WriteLine("{0} join lobby: {1}", clientPeer.NetPeer.EndPoint.ToString(), LobbyID);
                return true;
            }
            return false;
        }

        public void RemoveClientPeer(MasterPeer clientPeer)
        {
            if (ClientPeers.Contains(clientPeer))
            {
                ClientPeers.Remove(clientPeer);
                Console.WriteLine("{0} Leave lobby: {1}", clientPeer.NetPeer.EndPoint.ToString(), LobbyID);
                if (ClientPeers.Count <= 0)
                {
                    LobbyFactory.Instance.RemoveLobby(this);
                }
            }
            else
            {
                Console.WriteLine("{0} Leave lobby Failed, not exits in: {1}", clientPeer.NetPeer.EndPoint.ToString(), LobbyID);
            }
        }

        public Room? CreateRoom(MasterPeer clientPeer, string roomName, bool isVisible, bool needPassword, string password, int maxPlayers, Hashtable roomProperties)
        {
            Room lobbyRoom = new Room(this, clientPeer, roomName, isVisible, needPassword, password, maxPlayers, roomProperties);
            Rooms.Add(lobbyRoom);
            return lobbyRoom;
        }

        public Room? GetRoom(string roomID)
        {
            return Rooms.FirstOrDefault((a) => a.RoomID == roomID);
        }


        public void RemoveRoom(Room lobbyRoom)
        {
            if (Rooms.Contains(lobbyRoom))
            {
                Rooms.Remove(lobbyRoom);
            }
        }

        public void RemoveRoom(string roomID)
        {
            Room? lobbyRoom = Rooms.FirstOrDefault((a) => a.RoomID == roomID);
            if (lobbyRoom != null)
                RemoveRoom(lobbyRoom);
        }
    }
}
