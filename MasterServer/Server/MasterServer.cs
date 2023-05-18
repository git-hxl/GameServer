using LiteNetLib;
using MasterServer.Game;
using MasterServer.Operation;
using Serilog;
using SharedLibrary.Message;
using SharedLibrary.Operation;
using SharedLibrary.Server;

namespace MasterServer.Server
{
    internal class MasterServer : ServerBase
    {
        public static MasterServer Instance { get; set; }

        public Dictionary<int, MasterPeer> MasterPeers { get; private set; }

        public Dictionary<int, GamePeer> GamePeers { get; private set; }

        public List<RoomInfo> RoomInfos { get; set; }

        private OperationHandler operationHandler;

        public MasterServer(MasterConfig serverConfig) : base(serverConfig)
        {
            MasterPeers = new Dictionary<int, MasterPeer>();
            GamePeers = new Dictionary<int, GamePeer>();

            operationHandler = new OperationHandler();
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            base.OnConnectionRequest(request);
        }

        protected override void OnPeerConnected(NetPeer peer)
        {
            if (peer.EndPoint.Port == 8888)
            {
                GamePeers[peer.Id] = new GamePeer(peer);
                Log.Information("server peer connection: {0}", peer.EndPoint);
            }
            else
            {
                MasterPeers[peer.Id] = new MasterPeer(peer);
                Log.Information("peer connection: {0}", peer.EndPoint);
            }
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (MasterPeers.ContainsKey(peer.Id))
            {
                MasterPeers.Remove(peer.Id);
                Log.Information("peer disconnection: {0} info: {1}", peer.EndPoint, disconnectInfo.Reason.ToString());
            }
            if (GamePeers.ContainsKey(peer.Id))
            {
                GamePeers.Remove(peer.Id);
                Log.Information("server peer disconnection: {0} info: {1}", peer.EndPoint, disconnectInfo.Reason.ToString());
            }
        }

        protected override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                ServerOperationCode serverOperationCode;
                OperationCode operationCode;
                ReturnCode returnCode;
                switch (channel)
                {
                    case 0:
                        serverOperationCode = (ServerOperationCode)reader.GetByte();
                        operationHandler.OnServerRequest(serverOperationCode, GamePeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;
                    case 1:
                        serverOperationCode = (ServerOperationCode)reader.GetByte();
                        returnCode = (ReturnCode)reader.GetByte();
                        operationHandler.OnServerResponse(serverOperationCode, returnCode, GamePeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;

                    case 2:
                        operationCode = (OperationCode)reader.GetByte();
                        operationHandler.OnClientRequest(operationCode, MasterPeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;
                    case 3:
                        operationCode = (OperationCode)reader.GetByte();
                        returnCode = (ReturnCode)reader.GetByte();
                        operationHandler.OnClientResponse(operationCode, returnCode, MasterPeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error("peer receive error: {0}", ex.Message);
            }
        }


        public GamePeer GetLowerLoadLevelingServer()
        {
            return GamePeers.Values.OrderBy((a) => a.ServerInfo.CPU).ThenBy((a) => a.ServerInfo.Players).ThenBy((a) => a.ServerInfo.Memory).FirstOrDefault();
        }
    }
}