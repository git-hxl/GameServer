using LiteNetLib;
using LiteNetLib.Utils;
using Serilog;
using ShareLibrary;

namespace GameServer
{
    internal class GameApplication : ServerBase
    {
        private OperationHandlerDefault operationHandlerDefault;
        private NetPeer masterServer;
        public static GameApplication Instance { get; private set; } = new GameApplication();
        public GameServerConfig GameServerConfig { get; private set; }

        public override void Init(ServerConfig serverConfig)
        {
            base.Init(serverConfig);
            GameServerConfig = (GameServerConfig)serverConfig;
            operationHandlerDefault = new OperationHandlerDefault();

            masterServer = netManager.Connect(GameServerConfig.MasterIP, GameServerConfig.MasterPort, GameServerConfig.connectKey);
        }

        private  void RegisterGameServer()
        {
            RegisterGameServerRequest request = new RegisterGameServerRequest();
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            SendToMaster(OperationCode.RegisterGameServer, data);
        }

        protected override void NetListener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (netManager.ConnectedPeersCount <= GameServerConfig.maxPeers)
                request.AcceptIfKey(GameServerConfig.connectKey);
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
            if(peer.EndPoint.Address.ToString().Equals(GameServerConfig.MasterIP))
            {
                RegisterGameServer();
            }

            Log.Information("peer connected: {0} id:{1} total:{2}", peer.EndPoint, peer.Id, netManager.ConnectedPeersCount);
        }


        protected override void NetListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (peer.EndPoint.Address.ToString().Equals(GameServerConfig.MasterIP))
            {
                Log.Error("master server connect failed");
            }

            Log.Information("peer disconnected: {0} id:{1} total:{2}", peer.EndPoint, peer.Id, netManager.ConnectedPeersCount);

        }

        public void SendToMaster(OperationCode operationCode, byte[] data)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)operationCode);
            if (data != null && data.Length > 0)
            {
                netDataWriter.Put(data);
            }
            masterServer.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }
    }
}
