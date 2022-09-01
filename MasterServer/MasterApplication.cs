using LiteNetLib;
using MasterServer.GameServer;
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
        public void Init(MasterServerConfig serverConfig)
        {
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
            gameServer.Start(serverConfig.portForGame);

            Log.Information("start server: {0} {1}", serverConfig.port, serverConfig.portForGame);
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
            Log.Information("game server connected:{0}",peer.EndPoint.ToString());
        }

        private void GameServerListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            GameServerPeer? peerPeer;
            serverPeers.Remove(peer.Id, out peerPeer);
            Log.Information("game server disconnected:{0} info:{1}", peer.EndPoint.ToString(),disconnectInfo.Reason.ToString());
        }


        private void GameServerListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
             
        }


        private void ClientServerListener_ConnectionRequestEvent(ConnectionRequest request)
        {
            throw new NotImplementedException();
        }
        private void ClientServerListener_PeerConnectedEvent(NetPeer peer)
        {
            throw new NotImplementedException();
        }

        private void ClientServerListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            throw new NotImplementedException();
        }

        private void ClientServerListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            throw new NotImplementedException();
        }


        protected override void NetListener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (netManager.ConnectedPeersCount <= MasterServerConfig.maxPeers)
                request.AcceptIfKey(MasterServerConfig.connectKey);
            else
            {
                request.Reject();
                Log.Information("Reject Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }

        protected override void NetListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                OperationCode operationCode = (OperationCode)reader.GetByte();
                OperationRequest operationRequest = new OperationRequest(operationCode, reader.GetRemainingBytes(), deliveryMethod);
                OperationResponse operationResponse = operationHandlerDefault.OnOperationRequest(peer, operationRequest);
                operationResponse.SendTo(peer);
            }
            catch (Exception e)
            {
                Log.Information(e.ToString());
            }
        }


        protected override void NetListener_PeerConnectedEvent(NetPeer peer)
        {
            Log.Information("peer connected: {0} id:{1} total:{2}", peer.EndPoint, peer.Id, PlayerCache.Instance.Count);
        }


        protected override void NetListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            PlayerCache.Instance.RemovePlayer(peer.Id);
            GameServerCache.Instance.RemoveGameServer(peer.Id);
            Log.Information("peer disconnected: {0} id:{1} total:{2}", peer.EndPoint, peer.Id, PlayerCache.Instance.Count);
        }
    }
}
