using CommonLibrary.Table;
using LiteNetLib;
namespace MasterServer
{
    public sealed class MasterPeer
    {
        public UserTable User { get; private set; }
        public NetPeer NetPeer { get; private set; }

        public Lobby? CurLobby { get; private set; }
        public Room? CurRoom { get; private set; }

        public bool IsInLooby { get { return CurLobby != null; } }
        public bool IsInRoom { get { return CurRoom != null; } }
        public bool IsMaster { get; private set; }
        public MasterPeer(NetPeer netPeer, UserTable user)
        {
            NetPeer = netPeer;
            User = user;
        }

        public void OnJoinLobby(Lobby lobby)
        {
            CurLobby = lobby;
        }

        public void OnLeaveLobby()
        {
            CurLobby = null;
        }

        public void OnJoinRoom(Room room)
        {
            CurRoom = room;
        }

        public void OnLeaveRoom()
        {
            CurRoom = null;
        }

        public void OnDisConnected()
        {
            OnLeaveRoom();
            OnLeaveLobby();
        }
    }
}
