using LiteNetLib;
using MasterServer.Lobby;
using Serilog;
using System.Collections;

namespace MasterServer
{
    public sealed class MasterPeer
    {
        public int UserID { get; set; }

        public NetPeer NetPeer { get; private set; }

        private Lobby.Lobby? curLobby;
        private LobbyRoom? curRoom;

        public MasterPeer(NetPeer netPeer)
        {
            this.NetPeer = netPeer;
        }

        public void Login(int userID)
        {
            this.UserID = userID;
            //join lobby
            JoinLobby();
        }

        public bool JoinLobby(string lobbyName = "Default")
        {
            LeaveLobby();
            if (curLobby == null)
            {
                curLobby = LobbyFactory.Instance.GetOrCreateLobby(lobbyName);
                if (curLobby != null)
                {
                    return curLobby.AddClientPeer(this);
                }
            }
            return false;
        }

        public void LeaveLobby()
        {
            if (curLobby != null)
            {
                curLobby.RemoveClientPeer(this);
                curLobby = null;
            }
        }

        public LobbyRoom? CreateRoom(string roomName, bool isVisible, string password, int maxPeers, Hashtable roomProperties)
        {
            if (curLobby != null && curRoom == null)
            {
                LobbyRoom? lobbyRoom = curLobby.CreateRoom(this, roomName, isVisible, password, maxPeers, roomProperties);
                curRoom = lobbyRoom;
                return curRoom;
            }
            return null;
        }

        public LobbyRoom? JoinRoom(string roomID, string password)
        {
            LobbyRoom? lobbyRoom = curLobby?.GetRoom(roomID);

            if (curRoom == null && lobbyRoom != null && lobbyRoom.IsVisible && lobbyRoom.Password.Equals(password))
            {
                if (lobbyRoom.AddClientPeer(this))
                {
                    curRoom = lobbyRoom;
                    return curRoom;
                }
            }
            return null;
        }

        public bool LeaveRoom()
        {
            if (curRoom != null)
            {
                curRoom.RemoveClientPeer(this);
                curRoom = null;
                return true;
            }
            return false;
        }

        public void OnDisConnected()
        {
            LeaveRoom();
            if (curLobby != null)
            {
                curLobby.RemoveClientPeer(this);
            }
        }
    }
}
