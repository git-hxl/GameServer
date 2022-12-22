using LiteNetLib;
using Serilog;
using System.Net;

namespace SharedLibrary.Server
{
    public abstract class ServerBase
    {
        protected NetManager netManager;
        protected EventBasedNetListener eventBasedNetListener;
        public ServerConfig ServerConfig { get; private set; }
        public ServerBase(ServerConfig serverConfig)
        {
            ServerConfig = serverConfig;

            eventBasedNetListener = new EventBasedNetListener();

            eventBasedNetListener.ConnectionRequestEvent += OnConnectionRequest;
            eventBasedNetListener.PeerConnectedEvent += OnPeerConnected;
            eventBasedNetListener.PeerDisconnectedEvent += OnPeerDisconnected;
            eventBasedNetListener.NetworkReceiveEvent += OnNetworkReceive;

            netManager = new NetManager(eventBasedNetListener);
            netManager.PingInterval = serverConfig.PingInterval;
            netManager.DisconnectTimeout = serverConfig.DisconnectTimeout;
            netManager.ReconnectDelay = serverConfig.ReconnectDelay;
            netManager.MaxConnectAttempts = serverConfig.MaxConnectAttempts;
            InitLog();
        }

        protected virtual void InitLog()
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration.WriteTo.File("./Log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).WriteTo.Console();
            loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();
        }

        public virtual void Start()
        {
            netManager.Start(ServerConfig.Port);
            Log.Information("start server:{0}", netManager.LocalPort);
        }

        public void Update()
        {
            netManager.PollEvents();
        }

        protected abstract void OnPeerConnected(NetPeer peer);
        protected abstract void OnConnectionRequest(ConnectionRequest request);
        protected abstract void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo);
        protected abstract void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod);
    }
}