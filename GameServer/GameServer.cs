using GameServer.Operation;
using LiteNetLib;
using Serilog;
using SharedLibrary.Operation;
using SharedLibrary.Server;

namespace GameServer
{
    internal class GameServer : ServerBase
    {
        public GameConfig GameConfig { get; private set; }

        public Dictionary<int, GamePeer> GamePeers = new Dictionary<int, GamePeer>();
        public OperationHandler OperationHandler { get; private set; } = new OperationHandler();
        public GameServer(GameConfig serverConfig) : base(serverConfig)
        {
            GameConfig = serverConfig;
        }

        public override async void Start()
        {
            base.Start();
        }


        private void RegisterServer()
        {

        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            if (request.Data.GetString() == ServerConfig.ServerConnectKey)
                request.AcceptIfKey(ServerConfig.ServerConnectKey);
            else if (netManager.ConnectedPeersCount < ServerConfig.MaxPeers)
                request.AcceptIfKey(ServerConfig.ClientConnectKey);
            else
                request.Reject();
        }

        protected override void OnPeerConnected(NetPeer peer)
        {
            GamePeers[peer.Id] = new GamePeer(peer);
            Log.Information("peer connection: {0}", peer.EndPoint);
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            GamePeers.Remove(peer.Id);
            Log.Information("peer disconnection: {0} info: {1}", peer.EndPoint, disconnectInfo.Reason.ToString());
        }

        protected override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                OperationType operationType = (OperationType)reader.GetByte();
                OperationCode operationCode = (OperationCode)reader.GetByte();
                switch (operationType)
                {
                    case OperationType.Request:
                        OperationHandler.OnRequest(operationCode, GamePeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;
                    case OperationType.Response:
                        ReturnCode returnCode = (ReturnCode)reader.GetByte();
                        OperationHandler.OnResponse(operationCode, returnCode, GamePeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error("peer receive error: {0}", ex.Message);
            }
        }

    }
}
