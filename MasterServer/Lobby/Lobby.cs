using LiteNetLib;
using System.Collections;
using System.Linq;

namespace MasterServer.Lobby
{
    public class Lobby
    {
        public string LobbyName { get; }
        public ushort MaxPeers { get; }
        public ushort MaxRooms { get; }

        private List<ClientPeer> clientPeers = new List<ClientPeer>();
        private List<LobbyRoom> lobbyRooms = new List<LobbyRoom>();
        public Lobby(string lobbyName)
        {
            this.LobbyName = lobbyName;
            MaxPeers = 5000;
            MaxRooms = 1000;
        }

        public bool IsFullLobby => clientPeers.Count >= MaxPeers;
        public bool IsFullRoom => lobbyRooms.Count >= MaxRooms;

        public bool AddClientPeer(ClientPeer clientPeer)
        {
            if (!clientPeers.Contains(clientPeer))
            {
                clientPeers.Add(clientPeer);
                Console.WriteLine("{0} join lobby: {1}", clientPeer.NetPeer.EndPoint.ToString(), LobbyName);
                return true;
            }
            return false;
        }

        public void RemoveClientPeer(ClientPeer clientPeer)
        {
            if (clientPeers.Contains(clientPeer))
            {
                clientPeers.Remove(clientPeer);
                Console.WriteLine("{0} Leave lobby: {1}", clientPeer.NetPeer.EndPoint.ToString(), LobbyName);
                if (clientPeers.Count <= 0)
                {
                    LobbyFactory.Instance.RemoveLobby(this);
                }
            }
            else
            {
                Console.WriteLine("{0} Leave lobby Failed, not exits in: {1}", clientPeer.NetPeer.EndPoint.ToString(), LobbyName);
            }
        }

        public LobbyRoom? CreateRoom(ClientPeer clientPeer, string roomName, bool isVisible, string password,int maxPlayers, Hashtable roomProperties)
        {
            LobbyRoom lobbyRoom = new LobbyRoom(this, clientPeer, roomName,isVisible,password, maxPlayers, roomProperties);
            lobbyRooms.Add(lobbyRoom);
            return lobbyRoom;
        }

        public LobbyRoom? GetRoom(string roomID)
        {
            return lobbyRooms.FirstOrDefault((a) => a.RoomID == roomID);
        }


        public void RemoveRoom(LobbyRoom lobbyRoom)
        {
            if(lobbyRooms.Contains(lobbyRoom))
            {
                lobbyRooms.Remove(lobbyRoom);
            }
        }

        public void RemoveRoom(string roomID)
        {
            LobbyRoom? lobbyRoom = lobbyRooms.FirstOrDefault((a) => a.RoomID == roomID);
            if (lobbyRoom != null)
            {
                lobbyRooms.Remove(lobbyRoom);
            }
        }
    }
}
