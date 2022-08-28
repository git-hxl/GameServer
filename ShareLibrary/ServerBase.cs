using LiteNetLib;
using Newtonsoft.Json;
using Serilog;

namespace ShareLibrary
{
    public abstract class ServerBase
    {
        protected NetManager netManager;
        protected EventBasedNetListener netListener;

        public ServerConfig ServerConfig { get; protected set; }
        public ServerBase()
        {
            string json = File.ReadAllText("./ServerConfig.json");
            ServerConfig = JsonConvert.DeserializeObject<ServerConfig>(json);
            if (ServerConfig == null)
                ServerConfig = new ServerConfig();

            netListener = new EventBasedNetListener();

            netListener.ConnectionRequestEvent += NetListener_ConnectionRequestEvent;
            netListener.PeerConnectedEvent += NetListener_PeerConnectedEvent;
            netListener.PeerDisconnectedEvent += NetListener_PeerDisconnectedEvent;
            netListener.NetworkReceiveEvent += NetListener_NetworkReceiveEvent;

            netManager = new NetManager(netListener);
            netManager.PingInterval = ServerConfig.pingInterval;
            netManager.DisconnectTimeout = ServerConfig.disconnectTimeout;
            netManager.ReconnectDelay = ServerConfig.reconnectDelay;
            netManager.MaxConnectAttempts = ServerConfig.maxConnectAttempts;

            netManager.UnsyncedEvents = true;

            netManager.Start(ServerConfig.port);

            Log.Information("start server: {0}", ServerConfig.port);
        }

        public virtual void Close()
        {
            if (netManager != null)
                netManager.Stop();
        }

        public virtual void Update()
        {
            if (netManager != null)
            {
                netManager.PollEvents();
            }
        }

        protected abstract void NetListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod);
        protected abstract void NetListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo);
        protected abstract void NetListener_PeerConnectedEvent(NetPeer peer);
        protected abstract void NetListener_ConnectionRequestEvent(ConnectionRequest request);
    }
}
