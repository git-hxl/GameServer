using LiteNetLib;
using MasterServer.Operation;
using Serilog;
using SharedLibrary.Operation;
using SharedLibrary.Server;

namespace MasterServer
{
    internal class MasterServer : ServerBase
    {
        public MasterConfig MasterConfig { get; private set; }

        public Dictionary<int, MasterPeer> MasterPeers { get; private set; } = new Dictionary<int, MasterPeer>();

        public OperationHandler OperationHandler { get; private set; } = new OperationHandler();
        public MasterServer(MasterConfig serverConfig) : base(serverConfig)
        {
            MasterConfig = serverConfig;
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
            MasterPeers[peer.Id] = new MasterPeer(peer);
            Log.Information("peer connection: {0}", peer.EndPoint);
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            MasterPeers.Remove(peer.Id);
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
                        OperationHandler.OnRequest(operationCode, MasterPeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;
                    case OperationType.Response:
                        ReturnCode returnCode = (ReturnCode)reader.GetByte();
                        OperationHandler.OnResponse(operationCode, returnCode, MasterPeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
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