using LiteNetLib;
using MasterServer;
using Serilog;
using System.Collections.Concurrent;

namespace GameServer
{
    internal class GameApplication
    {
        public static GameApplication Instance { get; private set; } = new GameApplication();
        public GameServerConfig GameServerConfig { get; private set; }

        private NetManager gameServer;

        private EventBasedNetListener gameServerListener;

        private ConcurrentDictionary<int, GameClientPeer> clientPeers = new ConcurrentDictionary<int, GameClientPeer>();

        private OperationHandler operationHandler = new OperationHandler();

        public void Init(GameServerConfig serverConfig)
        {
            this.GameServerConfig = serverConfig; 
            gameServerListener = new EventBasedNetListener();

            gameServerListener.ConnectionRequestEvent += GameServerListener_ConnectionRequestEvent;
            gameServerListener.PeerConnectedEvent += GameServerListener_PeerConnectedEvent;
            gameServerListener.PeerDisconnectedEvent += GameServerListener_PeerDisconnectedEvent;
            gameServerListener.NetworkReceiveEvent += GameServerListener_NetworkReceiveEvent;

            gameServer = new NetManager(gameServerListener);
            gameServer.PingInterval = serverConfig.pingInterval;
            gameServer.DisconnectTimeout = serverConfig.disconnectTimeout;
            gameServer.ReconnectDelay = serverConfig.reconnectDelay;
            gameServer.MaxConnectAttempts = serverConfig.maxConnectAttempts;
            gameServer.UnsyncedEvents = true;

            gameServer.Start(serverConfig.port);
            gameServer.Connect(serverConfig.masterIP, serverConfig.innerPort, serverConfig.connectKey);
            Log.Information("start server: {0} register to {1}", serverConfig.port, serverConfig.masterIP);
        }

        private void GameServerListener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (gameServer.ConnectedPeersCount <= GameServerConfig.maxPeers)
                request.AcceptIfKey(GameServerConfig.connectKey);
            else
            {
                request.Reject();
                Log.Information("Reject client Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }

        private void GameServerListener_PeerConnectedEvent(NetPeer peer)
        {
            GameClientPeer clientPeer = new GameClientPeer(peer);
            clientPeers[peer.Id] = clientPeer;
            if (peer.Id == 0)
            {
                Log.Information("game connected to master :{0}", peer.EndPoint.ToString());

                clientPeer.RegisterToMaster();
            }
            else
            {
                Log.Information("client connected:{0}", peer.EndPoint.ToString());
            }
        }

        private void GameServerListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (peer.Id == 0)
            {
                Log.Information("register failed:{0} info:{1}", peer.EndPoint.ToString(), disconnectInfo.Reason.ToString());
                System.Environment.Exit(0);
            }
            else
            {
                GameClientPeer? clientPeer;
                clientPeers.Remove(peer.Id, out clientPeer);
                Log.Information("client disconnected:{0} info:{1}", peer.EndPoint.ToString(), disconnectInfo.Reason.ToString());
            }
        }


        private void GameServerListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                byte operationType = reader.GetByte();
                if(operationType == 0)
                {
                    OperationCode operationCode = (OperationCode)reader.GetByte();
                    OperationRequest operationRequest = new OperationRequest(operationCode, reader.GetRemainingBytes(), deliveryMethod);
                    OperationResponse operationResponse = operationHandler.OnOperationRequest(clientPeers[peer.Id], operationRequest);
                    operationResponse.SendTo(peer);
                }
                else if (operationType == 1)
                {
                    OperationCode operationCode = (OperationCode)reader.GetByte();
                    ReturnCode returnCode = (ReturnCode)reader.GetByte();
                    OperationResponse operationResponse = new OperationResponse(operationCode,returnCode, reader.GetRemainingBytes(), deliveryMethod);
                    operationHandler.OnOperationResponse(clientPeers[peer.Id], operationResponse);
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
        }
    }
}