using LiteNetLib;
namespace CoreLibrary
{
    public abstract class ServerBase
    {
        protected NetManager netManager;
        protected EventBasedNetListener netListener;

        protected ServerConfig serverConfig;
        public ServerBase(ServerConfig serverConfig)
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

            this.serverConfig = serverConfig;
        }

        protected abstract void NetListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod);
        protected abstract void NetListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo);
        protected abstract void NetListener_PeerConnectedEvent(NetPeer peer);
        protected abstract void NetListener_ConnectionRequestEvent(ConnectionRequest request);
        protected abstract void OnOperationRequest(NetPeer peer, OperationRequest operationRequest);
    }
}