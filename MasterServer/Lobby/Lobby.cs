using LiteNetLib;
namespace MasterServer.Lobby
{
    public class Lobby
    {
        public string LobbyName { get; } = "";
        public byte MaxPeers { get; }
        private Dictionary<string, ClientPeer> clientPeers = new Dictionary<string, ClientPeer>();

        public Lobby(string lobbyName)
        {
            this.LobbyName = lobbyName;
            MaxPeers = 255;
        }

        public bool IsFull => clientPeers.Values.Count >= MaxPeers;

        public void JoinClientPeer(ClientPeer clientPeer)
        {
            if (!clientPeers.ContainsKey(clientPeer.UserID))
            {
                clientPeers.Add(clientPeer.UserID, clientPeer);
                clientPeer.OnJoinLobby(this);
                Console.WriteLine("{0} join lobby: {1}", clientPeer.NetPeer.EndPoint.ToString(), LobbyName);
            }
            else
            {
                Console.WriteLine("{0} join lobby Failed: {1}", clientPeer.NetPeer.EndPoint.ToString(), LobbyName);
            }

        }

        public void RemoveClientPeer(ClientPeer clientPeer)
        {
            if (clientPeers.ContainsKey(clientPeer.UserID))
            {
                clientPeers.Remove(clientPeer.UserID);
                clientPeer.OnLeaveLobby(this);
                Console.WriteLine("{0} Leave lobby: {1}", clientPeer.NetPeer.EndPoint.ToString(), LobbyName);
            }
            else
            {
                Console.WriteLine("{0} Leave lobby Failed, not exits in: {1}", clientPeer.NetPeer.EndPoint.ToString(), LobbyName);
            }

        }
    }
}
