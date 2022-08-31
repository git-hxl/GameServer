using LiteNetLib;
using Newtonsoft.Json;
using Serilog;

namespace ShareLibrary
{
    public abstract class ServerBase
    {
        protected NetManager netManager;
        protected EventBasedNetListener netListener;
        public virtual void Init(ServerConfig serverConfig)
        {
            netListener = new EventBasedNetListener();

            netListener.ConnectionRequestEvent += NetListener_ConnectionRequestEvent;
            netListener.PeerConnectedEvent += NetListener_PeerConnectedEvent;
            netListener.PeerDisconnectedEvent += NetListener_PeerDisconnectedEvent;
            netListener.NetworkReceiveEvent += NetListener_NetworkReceiveEvent;

            netManager = new NetManager(netListener);
            netManager.PingInterval = serverConfig.pingInterval;
            netManager.DisconnectTimeout = serverConfig.disconnectTimeout;
            netManager.ReconnectDelay = serverConfig.reconnectDelay;
            netManager.MaxConnectAttempts = serverConfig.maxConnectAttempts;

            netManager.UnsyncedEvents = true;

            netManager.Start(serverConfig.port);

            Log.Information("start server: {0}", serverConfig.port);
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
