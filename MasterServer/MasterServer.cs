using CoreLibrary;
using LiteNetLib;
using Serilog;

namespace MasterServer
{
    internal class MasterServer : ServerBase
    {
        private OperationHandlerBase operationHandlerBase;
        public MasterServer(ServerConfig serverConfig) : base(serverConfig)
        {
            operationHandlerBase = new OperationHandlerBase();
        }

        protected override void NetListener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (netManager.ConnectedPeersCount <= serverConfig.maxPeers)
                request.AcceptIfKey(serverConfig.connectKey);
            else
            {
                request.Reject();
                Log.Information("NetListener_ConnectionRequestEvent:{0}", request.RemoteEndPoint);
            }
        }

        protected override void NetListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            throw new NotImplementedException();
        }

        protected override void NetListener_PeerConnectedEvent(NetPeer peer)
        {
            Log.Information("NetListener_PeerConnectedEvent:{0}", peer.EndPoint);
        }

        protected override void NetListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Information("NetListener_PeerDisconnectedEvent:{0} SocketErrorCode:{1}", peer.EndPoint, disconnectInfo.SocketErrorCode);
        }

        protected override void OnOperationRequest(NetPeer peer, OperationRequest operationRequest)
        {
            throw new NotImplementedException();
        }
    }
}
