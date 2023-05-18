using LiteNetLib;
using Serilog;
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

            netManager.ChannelsCount = 4;

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

        public virtual void Update()
        {
            netManager.PollEvents();
        }

        protected abstract void OnPeerConnected(NetPeer peer);
        protected virtual void OnConnectionRequest(ConnectionRequest request)
        {
            if (netManager.ConnectedPeersCount < ServerConfig.MaxPeers)
            {
                request.AcceptIfKey(ServerConfig.ConnectKey);
            }
        }
        protected abstract void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="reader"></param>
        /// <param name="channel">[0,1]服务器内部通信通道，[2,3]外部客户端通信通道</param>
        /// <param name="deliveryMethod"></param>
        protected abstract void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod);
    }
}
