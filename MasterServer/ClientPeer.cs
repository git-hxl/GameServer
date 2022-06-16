using LiteNetLib;
namespace MasterServer
{
    public sealed class ClientPeer
    {
        public string UserID { get; }
        public NetPeer NetPeer { get; }
        public bool IsConnected { get; }

        public ClientPeer(string userID, NetPeer netPeer)
        {
            this.UserID = userID;
            this.NetPeer = netPeer;
            this.IsConnected = true;
        }

        public void OnJoinLobby(Lobby.Lobby lobby)
        {

        }

        public void OnLeaveLobby(Lobby.Lobby lobby)
        {

        }
    }
}
