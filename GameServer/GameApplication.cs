using CommonLibrary.Core;
using GameServer.Operations;
using LiteNetLib;
using Serilog;
namespace GameServer
{
    public class GameApplication : ApplicationBase
    {
        public List<Game> Games { get; private set; }
        public Dictionary<int, GamePeer> GamePeers { get; private set; } = new Dictionary<int, GamePeer>();
        public static GameApplication Instance { get; private set; } = new GameApplication();
        public GameOperationHandle GameOperationHandle { get; private set; } = new GameOperationHandle();
        public GameApplication() : base("./GameServerConfig.json")
        {
            Games = new List<Game>();
        }

        protected override void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (ServerConfig != null && server.ConnectedPeersCount < ServerConfig.MaxPeers)
                request.AcceptIfKey(ServerConfig.ConnectKey);
            else
            {
                request.Reject();
                Log.Information("Reject Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }

        protected override void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Log.Information("We got connection: {0} id:{1}", peer.EndPoint, peer.Id);
        }

        protected override void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Information("We got disconnection: {0}", peer.EndPoint);

            if (GamePeers.ContainsKey(peer.Id))
            {
                GamePeers[peer.Id].OnDisConnected();
                GamePeers.Remove(peer.Id);
            }
        }

        protected override void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            OperationCode operationCode = (OperationCode)reader.GetByte();
            MsgPack msgPack = MessagePack.MessagePackSerializer.Deserialize<MsgPack>(reader.GetRemainingBytes());
            HandleRequest handleRequest = new HandleRequest(peer, msgPack, deliveryMethod);
            GameOperationHandle.HandleRequest(operationCode, handleRequest);
        }

        public void AddClientPeer(GamePeer gamePeer)
        {
            GamePeers[gamePeer.NetPeer.Id] = gamePeer;
        }

        public GamePeer? GetClientPeer(int id)
        {
            if (GamePeers.ContainsKey(id))
            {
                return GamePeers[id];
            }
            return null;
        }

        public Game? CreateGame(string roomID)
        {
            Game game = new Game(roomID);
            Games.Add(game);
            return game;
        }

        public Game? GetGame(string roomID)
        {
            return Games.FirstOrDefault((a) => a.GameID == roomID);
        }


        public void RemoveGame(Game game)
        {
            if (Games.Contains(game))
            {
                Games.Remove(game);
            }
        }

        public void RemoveGame(string roomID)
        {
            Game? game = Games.FirstOrDefault((a) => a.GameID == roomID);
            if (game != null)
                RemoveGame(game);
        }
    }
}
