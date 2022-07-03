using CommonLibrary.Operations;
using LiteNetLib;
using MasterServer.Lobby;
using MasterServer.Operations;
using System.Collections;

namespace MasterServer
{
    public sealed class ClientPeer
    {
        public int UserID { get; set; }
        public bool IsLogin { get; set; }
        public bool IsInLobby { get; set; }
        public string LobbyName { get; set; }

        public bool IsInRoom { get; set; }
        public string RoomID { get; set; }

        public NetPeer NetPeer { get; private set; }

        private OperationHandleBase handle;
        public ClientPeer(NetPeer netPeer)
        {
            this.LobbyName = "";
            this.RoomID = "";
            this.NetPeer = netPeer;
            this.handle = new OperationHandleBase();
        }

        public void Login(int userID)
        {
            this.UserID = userID;
            this.IsLogin = true;
        }

        public bool JoinLobby(string lobbyName)
        {
            if (IsLogin && !IsInRoom && !string.IsNullOrEmpty(lobbyName))
            {
                if (this.LobbyName != lobbyName)
                {
                    LeaveLobby();
                    Lobby.Lobby? lobby = LobbyFactory.Instance.GetOrCreateLobby(lobbyName);
                    if (lobby != null)
                    {
                        lobby.AddClientPeer(this);
                        this.LobbyName = lobbyName;
                        this.IsInLobby = true;
                        return true;
                    }
                }
            }
            return false;
        }

        public void LeaveLobby()
        {
            if (IsLogin && IsInLobby && !IsInRoom && !string.IsNullOrEmpty(this.LobbyName))
            {
                Lobby.Lobby? lobby = LobbyFactory.Instance.GetLobby(LobbyName);
                if (lobby != null)
                {
                    lobby.RemoveClientPeer(this);
                    this.IsInLobby = false;
                    this.LobbyName = "";
                }
                else
                {
                    Console.WriteLine("Leave Lobby error,No existed lobby");
                }
            }
        }

        public LobbyRoom? CreateRoom(string roomName, int maxPeers, Hashtable roomProperties)
        {
            if (!string.IsNullOrEmpty(roomName) && IsInLobby && !IsInRoom)
            {
                Lobby.Lobby? lobby = LobbyFactory.Instance.GetLobby(LobbyName);
                if (lobby != null)
                {
                    LobbyRoom? lobbyRoom = lobby.CreateRoom(this, roomName, maxPeers, roomProperties);
                    if (lobbyRoom != null)
                    {
                        this.IsInRoom = true;
                        this.RoomID = lobbyRoom.RoomID;
                        return lobbyRoom;
                    }
                }
            }
            return null;
        }

        public LobbyRoom? JoinRoom(string roomID)
        {
            if (!string.IsNullOrEmpty(roomID) && IsInLobby && !IsInRoom)
            {
                Lobby.Lobby? lobby = LobbyFactory.Instance.GetLobby(LobbyName);
                if (lobby != null)
                {
                    var lobbyRoom = lobby.GetRoom(roomID);
                    if (lobbyRoom != null)
                    {
                        if (JoinRoom(lobbyRoom))
                            return lobbyRoom;
                    }
                }
            }
            return null;
        }

        private bool JoinRoom(LobbyRoom lobbyRoom)
        {
            if (lobbyRoom != null)
            {
                if (lobbyRoom.AddClientPeer(this))
                {
                    this.IsInRoom = true;
                    this.RoomID = lobbyRoom.RoomID;
                    return true;
                }
            }
            return false;
        }

        public bool LeaveRoom()
        {
            if (IsLogin && IsInLobby && IsInRoom)
            {
                Lobby.Lobby? lobby = LobbyFactory.Instance.GetLobby(LobbyName);
                if (lobby != null)
                {
                    var lobbyRoom = lobby.GetRoom(RoomID);
                    if (lobbyRoom != null)
                    {
                        lobbyRoom.RemoveClientPeer(this);
                        this.IsInRoom = false;
                        this.RoomID = "";
                        return true;
                    }
                }
            }
            return false;
        }

        public void OnDisConnected()
        {
            LeaveLobby();
        }

        public void HandleRequest(OperationCode operationCode, byte[] requestData)
        {
            HandleRequest handleRequest = new HandleRequest(this, operationCode, requestData);

            handle.HandleRequest(handleRequest);
        }

    }
}
