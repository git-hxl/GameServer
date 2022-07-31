using LiteNetLib;
using MessagePack;
using Serilog;
using System.Collections;
namespace MasterServer
{
    [MessagePackObject]
    public class LobbyProperty
    {
        [Key(0)]
        public string LobbyID = "";
        [Key(1)]
        public ushort MaxPeers;
    }

    public class Lobby
    {
        public LobbyProperty LobbyProperty { get; }
        public List<MasterPeer> ClientPeers { get; }
        public List<Room> Rooms { get; }
        public Lobby()
        {
            LobbyProperty = new LobbyProperty();
            LobbyProperty.LobbyID = Guid.NewGuid().ToString();
            LobbyProperty.MaxPeers = 1000;

            ClientPeers = new List<MasterPeer>();
            Rooms = new List<Room>();
        }

        public bool IsFullLobby => ClientPeers.Count >= LobbyProperty.MaxPeers;

        public bool AddClientPeer(MasterPeer clientPeer)
        {
            if (!ClientPeers.Contains(clientPeer))
            {
                ClientPeers.Add(clientPeer);
                Log.Information("{0} join lobby: {1}", clientPeer.NetPeer.EndPoint.ToString(),LobbyProperty.LobbyID);
                return true;
            }
            return false;
        }

        public void RemoveClientPeer(MasterPeer clientPeer)
        {
            if (ClientPeers.Contains(clientPeer))
            {
                ClientPeers.Remove(clientPeer);
                Log.Information("{0} Leave lobby: {1}", clientPeer.NetPeer.EndPoint.ToString(), LobbyProperty.LobbyID);
                if (ClientPeers.Count <= 0)
                {
                    LobbyFactory.Instance.RemoveLobby(this);
                }
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
            return Rooms.FirstOrDefault((a) => a.RoomProperty.RoomID == roomID);
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
            Room? lobbyRoom = Rooms.FirstOrDefault((a) => a.RoomProperty.RoomID == roomID);
            if (lobbyRoom != null)
                RemoveRoom(lobbyRoom);
        }
    }
}
