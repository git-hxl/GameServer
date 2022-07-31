using LiteNetLib;
using Newtonsoft.Json;
using Serilog;

namespace CommonLibrary.Core
{
    public abstract class ApplicationBase
    {
        protected NetManager server;
        protected EventBasedNetListener listener;
        public ServerBaseConfig? ServerConfig { get; protected set; }
        public string ServerConfigPath { get; protected set; }

        public ApplicationBase(string configPath)
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            ServerConfig = new ServerBaseConfig();
            ServerConfigPath = configPath;
            InitConfig();
        }

        private void InitConfig()
        {
            string config = File.ReadAllText(ServerConfigPath);
            if (!string.IsNullOrEmpty(config))
                ServerConfig = JsonConvert.DeserializeObject<ServerBaseConfig>(config);
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

        protected abstract void Listener_ConnectionRequestEvent(ConnectionRequest request);
        protected abstract void Listener_PeerConnectedEvent(NetPeer peer);
        protected abstract void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo);
        protected abstract void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod);
    }
}