using LiteNetLib;
using Serilog;
using System.Collections.Concurrent;

namespace MasterServer
{
    internal class MasterApplication
    {
        public static MasterApplication Instance { get; private set; } = new MasterApplication();
        public MasterServerConfig MasterServerConfig { get; private set; }

        private NetManager clientServer;
        private NetManager gameServer;

        private EventBasedNetListener clientServerListener;
        private EventBasedNetListener gameServerListener;

        private ConcurrentDictionary<int, GameServerPeer> serverPeers = new ConcurrentDictionary<int, GameServerPeer>();
        private ConcurrentDictionary<int, MasterClientPeer> clientPeers = new ConcurrentDictionary<int, MasterClientPeer>();

        private OperationHandler operationHandler = new OperationHandler();
        public void Init(MasterServerConfig serverConfig)
        {
            this.MasterServerConfig = serverConfig;
            clientServerListener = new EventBasedNetListener();
            gameServerListener = new EventBasedNetListener();

            clientServerListener.ConnectionRequestEvent += ClientServerListener_ConnectionRequestEvent;
            clientServerListener.PeerConnectedEvent += ClientServerListener_PeerConnectedEvent;
            clientServerListener.PeerDisconnectedEvent += ClientServerListener_PeerDisconnectedEvent;
            clientServerListener.NetworkReceiveEvent += ClientServerListener_NetworkReceiveEvent;

            gameServerListener.ConnectionRequestEvent += GameServerListener_ConnectionRequestEvent;
            gameServerListener.PeerConnectedEvent += GameServerListener_PeerConnectedEvent;
            gameServerListener.PeerDisconnectedEvent += GameServerListener_PeerDisconnectedEvent;
            gameServerListener.NetworkReceiveEvent += GameServerListener_NetworkReceiveEvent;

            clientServer = new NetManager(clientServerListener);
            clientServer.PingInterval = serverConfig.pingInterval;
            clientServer.DisconnectTimeout = serverConfig.disconnectTimeout;
            clientServer.ReconnectDelay = serverConfig.reconnectDelay;
            clientServer.MaxConnectAttempts = serverConfig.maxConnectAttempts;
            clientServer.UnsyncedEvents = true;

            gameServer = new NetManager(gameServerListener);
            gameServer.PingInterval = serverConfig.pingInterval;
            gameServer.DisconnectTimeout = serverConfig.disconnectTimeout;
            gameServer.ReconnectDelay = serverConfig.reconnectDelay;
            gameServer.MaxConnectAttempts = serverConfig.maxConnectAttempts;
            gameServer.UnsyncedEvents = true;

            clientServer.Start(serverConfig.port);
            gameServer.Start(serverConfig.innerPort);

            Log.Information("start server: {0} {1}", serverConfig.port, serverConfig.innerPort);
        }

        private void GameServerListener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (gameServer.ConnectedPeersCount <= MasterServerConfig.maxPeers)
                request.AcceptIfKey(MasterServerConfig.connectKey);
            else
            {
                request.Reject();
                Log.Information("Reject game Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }

        private void GameServerListener_PeerConnectedEvent(NetPeer peer)
        {
            serverPeers[peer.Id] = new GameServerPeer(peer);
            Log.Information("game server connected:{0}", peer.EndPoint.ToString());
        }

        private void GameServerListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            GameServerPeer? serverPeer;
            serverPeers.Remove(peer.Id, out serverPeer);
            Log.Information("game server disconnected:{0} info:{1}", peer.EndPoint.ToString(), disconnectInfo.Reason.ToString());
        }


        private void GameServerListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                byte operationType = reader.GetByte();
                if (operationType == 0)
                {
                    OperationCode operationCode = (OperationCode)reader.GetByte();
                    OperationRequest operationRequest = new OperationRequest(operationCode, reader.GetRemainingBytes(), deliveryMethod);
                    OperationResponse operationResponse = operationHandler.OnOperationRequest(serverPeers[peer.Id], operationRequest);
                    operationResponse.SendTo(peer);
                }
            }
            catch(Exception ex)
            {
                Log.Error("receive error: {0}",ex.Message);
            }
        }


        private void ClientServerListener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (clientServer.ConnectedPeersCount <= MasterServerConfig.maxPeers)
                request.AcceptIfKey(MasterServerConfig.connectKey);
            else
            {
                request.Reject();
                Log.Information("Reject client Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }
        private void ClientServerListener_PeerConnectedEvent(NetPeer peer)
        {
            clientPeers[peer.Id] = new MasterClientPeer(peer);
            Log.Information("client connected:{0}", peer.EndPoint.ToString());
        }

        private void ClientServerListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            MasterClientPeer? clientPeer;
            clientPeers.Remove(peer.Id, out clientPeer);
            Log.Information("client disconnected:{0} info:{1}", peer.EndPoint.ToString(), disconnectInfo.Reason.ToString());
        }

        private void ClientServerListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                byte operationType = reader.GetByte();
                if (operationType == 0)
                {
                    OperationCode operationCode = (OperationCode)reader.GetByte();
                    OperationRequest operationRequest = new OperationRequest(operationCode, reader.GetRemainingBytes(), deliveryMethod);
                    OperationResponse operationResponse = operationHandler.OnOperationRequest(clientPeers[peer.Id], operationRequest);
                    operationResponse.SendTo(peer);
                }
            }
            catch (Exception ex)
            {
                Log.Error("receive error: {0}", ex.Message);
            }
        }

        public void Update()
        {
            if (gameServer != null)
                gameServer.PollEvents();
            if (clientServer != null)
                clientServer.PollEvents();
        }
    }
}
