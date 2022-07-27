using CommonLibrary.Utils;
using GameServer.Operations;
using LiteNetLib;
using Newtonsoft.Json;
using Serilog;

namespace GameServer
{
    internal class GameApplication : Singleton<GameApplication>
    {
        private EventBasedNetListener listener;
        private OperationHandleBase operationHandle;
        private List<Game> games = new List<Game>();
        private Dictionary<string, GamePeer> gamePeers = new Dictionary<string, GamePeer>();
        public GameServerConfig? ServerConfig { get; private set; }
        public NetPeer? MasterServer { get; private set; }
        public NetManager Server { get; private set; }
        public GameApplication()
        {
            listener = new EventBasedNetListener();
            Server = new NetManager(listener);
            operationHandle = new OperationHandleBase();
            Log.Information("Load Config");
            try
            {
                string config = File.ReadAllText("./GameServerConfig.json");
                if (!string.IsNullOrEmpty(config))
                    ServerConfig = JsonConvert.DeserializeObject<GameServerConfig>(config);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            };
        }

        public void Start()
        {
            if (ServerConfig == null)
            {
                Log.Error("No Config Loaded!");
                return;
            }
            Server.PingInterval = 1000;
            Server.DisconnectTimeout = 5000;
            Server.ReconnectDelay = 500;
            //最大连接尝试次数
            Server.MaxConnectAttempts = 10;
            Server.UnsyncedEvents = true;
            Server.Start(ServerConfig.Port);

            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;

            Log.Information("Start listener Successed");
            Log.Information("Connect To Master");

            MasterServer = Server.Connect(ServerConfig.MasterIP, ServerConfig.MasterPort, "Hello");
        }

        public void Close()
        {
            if (Server != null)
                Server.Stop(true);
        }

        public void Update()
        {
            if (Server != null)
            {
                Server.PollEvents();
            }
        }

        private void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (Server != null && Server.ConnectedPeersCount < 5000)
            {
                //TODO:Token 
                request.Accept();
            }
            else
            {
                request.Reject();
                Log.Information("Reject Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }

        private void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Log.Information("We got connection: {0} id:{1}", peer.EndPoint, peer.Id);
            GamePeer gamePeer = new GamePeer(peer);
            gamePeers[peer.EndPoint.ToString()] = gamePeer;
        }

        private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Information("We got disconnection: {0}", peer.EndPoint);
            if (gamePeers.ContainsKey(peer.EndPoint.ToString()))
            {
                gamePeers[peer.EndPoint.ToString()].OnDisConnected();
                gamePeers.Remove(peer.EndPoint.ToString());
            }
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (gamePeers.ContainsKey(peer.EndPoint.ToString()))
            {
                GamePeer gamePeer = gamePeers[peer.EndPoint.ToString()];
                GameOperationCode operationCode = (GameOperationCode)reader.GetByte();
                HandleRequest handleRequest = new HandleRequest(gamePeer, operationCode, reader.GetRemainingBytes(), deliveryMethod);
                operationHandle.HandleRequest(handleRequest);
            }
            else
            {
                Log.Information("Client {0} not connected!", peer.EndPoint.ToString());
            }
        }

        public GamePeer? GetGamePeer(NetPeer netPeer)
        {
            if (gamePeers.ContainsKey(netPeer.EndPoint.ToString()))
            {
                return gamePeers[netPeer.EndPoint.ToString()];
            }
            return null;
        }

        public Game GetOrCreateGame(string id)
        {
            Game? game = games.FirstOrDefault((a) => a.GameID == id);
            if (game == null)
            {
                game = new Game(id);
                games.Add(game);
            }
            return game;
        }
    }
}
