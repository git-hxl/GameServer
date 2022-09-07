using GameServer.Client;
using LiteNetLib;
using Serilog;
using SharedLibrary.Operation;
using SharedLibrary.Server;
namespace GameServer
{
    public class GameServer : Server
    {
        private OperationHandler operationHandler;
        public GameServer(ServerConfig serverConfig) : base(serverConfig)
        {
            operationHandler = new OperationHandler();
        }

        protected override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                byte operationType = reader.GetByte();
                if (operationType == 0)
                {
                    OperationCode operationCode = (OperationCode)reader.GetByte();
                    OperationRequest operationRequest = new OperationRequest(operationCode, reader.GetRemainingBytes(), deliveryMethod);
                    operationHandler.OnOperationRequest(peer, operationRequest);
                }
            }
            catch (Exception e)
            {
                Log.Error("peer receive error: {0}", e.ToString());
            }

        }

        protected override void OnPeerConnected(NetPeer peer)
        {
            PlayerManager.Instance.AddClientPeer(peer.Id, new ClientPeer(peer));
            Log.Information("peer connected:{0} threadid:{1}", peer.EndPoint.ToString(), Thread.CurrentThread.ManagedThreadId);
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            ClientPeer? clientPeer = PlayerManager.Instance.RemoveClientPeer(peer.Id);
            if (clientPeer != null)
            {
                clientPeer.OnDisconnected();
            }
            Log.Information("peer disconnected:{0} info:{1} threadid:{2}", peer.EndPoint.ToString(), disconnectInfo.Reason.ToString(), Thread.CurrentThread.ManagedThreadId);
        }
    }
}
