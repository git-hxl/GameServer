using LiteNetLib;
using MasterServer.OperationHandler;
using Serilog;
using ShareLibrary;

namespace MasterServer
{
    internal class MasterApplication : ServerBase
    {
        private OperationHandlerMaster operationHandlerMaster;
        public static MasterApplication Instance { get; private set; } = new MasterApplication();

        public PlayerCache PlayerCache { get; private set; } = new PlayerCache();

        public MasterApplication() : base()
        {
            operationHandlerMaster = new OperationHandlerMaster();
        }

        protected override void NetListener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (netManager.ConnectedPeersCount <= ServerConfig.maxPeers)
                request.AcceptIfKey(ServerConfig.connectKey);
            else
            {
                request.Reject();
                Log.Information("Reject Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }

        protected override void NetListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            OperationCode operationCode = (OperationCode)reader.GetByte();

            MasterPeer masterPeer = PlayerCache.GetPeer(peer.Id);
            OperationRequest operationRequest = new OperationRequest(operationCode, reader.GetRemainingBytes(), deliveryMethod);
            OperationResponse operationResponse = operationHandlerMaster.OnOperationRequest(masterPeer, operationRequest);

            operationResponse.SendTo(peer);
        }

        protected override void NetListener_PeerConnectedEvent(NetPeer peer)
        {
            PlayerCache.AddPeer(peer.Id, new MasterPeer(peer));
            Log.Information("peer connected: {0} id:{1} total:{2}", peer.EndPoint, peer.Id, PlayerCache.Count);
        }

        protected override void NetListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            PlayerCache.RemovePeer(peer.Id);
            Log.Information("peer disconnected: {0} id:{1} total:{2}", peer.EndPoint, peer.Id, PlayerCache.Count);
        }
    }
}
