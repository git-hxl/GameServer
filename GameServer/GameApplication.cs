using CommonLibrary.Core;
using GameServer.Operations;
using LiteNetLib;
using Newtonsoft.Json;
using Serilog;
namespace GameServer
{
    public class GameApplication
    {
        public static GameApplication Instance { get; private set; } = new GameApplication();
        public NetManager server { get; private set; }
        public EventBasedNetListener listener { get; private set; }
        public GameServerConfig? ServerConfig { get; private set; }
        public Dictionary<NetPeer, GamePeer> GamePeers { get; private set; }
        public GameOperationHandle GameOperationHandle { get; private set; }

        public Dictionary<string,Game> Games { get; private set; }
        public GameApplication() : base()
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            GamePeers = new Dictionary<NetPeer, GamePeer>();
            GameOperationHandle = new GameOperationHandle();
            Games =new Dictionary<string, Game>();

            string config = File.ReadAllText("./GameServerConfig.json");
            if (!string.IsNullOrEmpty(config))
                ServerConfig = JsonConvert.DeserializeObject<GameServerConfig>(config);
        }

        public void Start()
        {
            if (ServerConfig == null)
            {
                Log.Error("No Config Loaded!");
                return;
            }
            server.PingInterval = ServerConfig.PingInterval;
            server.DisconnectTimeout = ServerConfig.DisconnectTimeout;
            server.ReconnectDelay = ServerConfig.ReconnectDelay;
            server.MaxConnectAttempts = ServerConfig.MaxConnectAttempts;

            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;

            server.Start(ServerConfig.Port);
            Log.Information("Start Master Server");
        }

        public void Close()
        {
            if (server != null)
                server.Stop(true);
        }

        public void Update()
        {
            if (server != null)
            {
                server.PollEvents();
            }
        }

        protected void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (ServerConfig != null && server.ConnectedPeersCount < ServerConfig.MaxPeers)
                request.AcceptIfKey(ServerConfig.ConnectKey);
            else
            {
                request.Reject();
                Log.Information("Reject Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }

        protected void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Log.Information("We got connection: {0} id:{1}", peer.EndPoint, peer.Id);
        }

        protected void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Information("We got disconnection: {0}", peer.EndPoint);

            if (GamePeers.ContainsKey(peer))
            {
                GamePeers[peer].OnDisConnected();
                GamePeers.Remove(peer);
            }

            Log.Information("peers count: {0}", GamePeers.Count);
        }

        protected void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            OperationCode operationCode = (OperationCode)reader.GetByte();
            MsgPack msgPack = MessagePack.MessagePackSerializer.Deserialize<MsgPack>(reader.GetRemainingBytes());
            HandleRequest handleRequest = new HandleRequest(peer, operationCode, msgPack, deliveryMethod);
            GameOperationHandle.HandleRequest(handleRequest);
        }

        public void AddClientPeer(NetPeer netPeer, GamePeer gamePeer)
        {
            GamePeers[netPeer] = gamePeer;
            Log.Information("peers count: {0}", GamePeers.Count);
        }

        public GamePeer? GetClientPeer(NetPeer netPeer)
        {
            if (GamePeers.ContainsKey(netPeer))
            {
                return GamePeers[netPeer];
            }
            return null;
        }

        public Game? CreateGame(string roomID)
        {
            Game game = new Game(roomID);
            Games.Add(roomID,game);
            return game;
        }

        public Game? GetGame(string roomID)
        {
            return Games[roomID];
        }


        public void RemoveGame(Game game)
        {
            RemoveGame(game.GameID);
        }

        public void RemoveGame(string roomID)
        {
            if(Games.ContainsKey(roomID))
            {
                Games.Remove(roomID);
            }
        }
    }
}
