using LiteNetLib;
using Serilog;

namespace SharedLibrary.Server
{
    public class Server
    {
        private NetManager netManager;

        private EventBasedNetListener eventBasedNetListener;

        public ServerConfig ServerConfig { get; private set; }
        public Server(ServerConfig serverConfig)
        {
            ServerConfig = serverConfig;

            eventBasedNetListener = new EventBasedNetListener();

            eventBasedNetListener.ConnectionRequestEvent += OnConnectionRequest;
            eventBasedNetListener.PeerConnectedEvent += OnPeerConnected;
            eventBasedNetListener.PeerDisconnectedEvent += OnPeerDisconnected;
            eventBasedNetListener.NetworkReceiveEvent += OnNetworkReceive;

            netManager = new NetManager(eventBasedNetListener);
            netManager.PingInterval = serverConfig.pingInterval;
            netManager.DisconnectTimeout = serverConfig.disconnectTimeout;
            netManager.ReconnectDelay = serverConfig.reconnectDelay;
            netManager.MaxConnectAttempts = serverConfig.maxConnectAttempts;
            netManager.UnsyncedEvents = true;
        }

        public void Start(int port)
        {
            netManager.Start(port);
            Log.Information("start server:{0}", netManager.LocalPort);
        }

        public void Start()
        {
            netManager.Start();
            Log.Information("start server:{0}", netManager.LocalPort);
        }

        public NetPeer Connect(string ip, int port, string key)
        {
            return netManager.Connect(ip, port, key);
        }

        public void Update()
        {
            netManager.PollEvents();
        }

        protected virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnPeerConnected(NetPeer peer)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnConnectionRequest(ConnectionRequest request)
        {
            if (netManager.ConnectedPeersCount <= ServerConfig.maxPeers)
                request.AcceptIfKey(ServerConfig.connectKey);
            else
            {
                request.Reject();
                Log.Information("Reject game Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }
    }
}
