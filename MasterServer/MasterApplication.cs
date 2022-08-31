using LiteNetLib;
using MasterServer.GameServer;
using Serilog;
using ShareLibrary;
namespace MasterServer
{
    internal class MasterApplication : ServerBase
    {
        private OperationHandlerDefault operationHandlerDefault;
        public static MasterApplication Instance { get; private set; } = new MasterApplication();

        public MasterServerConfig MasterServerConfig { get; private set; }
        public LobbyFactory LobbyFactory { get; private set; } = new LobbyFactory();

        public override void Init(ServerConfig serverConfig)
        {
            base.Init(serverConfig);
            MasterServerConfig = (MasterServerConfig)serverConfig;
            operationHandlerDefault = new OperationHandlerDefault();
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
