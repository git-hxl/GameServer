using CommonLibrary.Core;
using LiteNetLib;
using MasterServer.Operations;
using Serilog;

namespace MasterServer
{
    public class MasterApplication : ApplicationBase
    {
        public Dictionary<int, MasterPeer> MasterPeers { get; private set; } = new Dictionary<int, MasterPeer>();
        public static MasterApplication Instance { get; private set; } = new MasterApplication();
        public MasterOperationHandle MasterOperationHandle { get; private set; } = new MasterOperationHandle();
        public MasterApplication() : base("./MasterServerConfig.json")
        {}

        protected override void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (ServerConfig != null && server.ConnectedPeersCount < ServerConfig.MaxPeers)
                request.AcceptIfKey(ServerConfig.ConnectKey);
            else
            {
                request.Reject();
                Log.Information("Reject Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }

        protected override void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Log.Information("We got connection: {0} id:{1}", peer.EndPoint, peer.Id);
        }

        protected override void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Information("We got disconnection: {0}", peer.EndPoint);

            if (MasterPeers.ContainsKey(peer.Id))
            {
                MasterPeers[peer.Id].OnDisConnected();
                MasterPeers.Remove(peer.Id);
            }
        }

        protected override void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            OperationCode operationCode = (OperationCode)reader.GetByte();
            MsgPack msgPack = MessagePack.MessagePackSerializer.Deserialize<MsgPack>(reader.GetRemainingBytes());
            HandleRequest handleRequest = new HandleRequest(peer, msgPack, deliveryMethod);
            MasterOperationHandle.HandleRequest(operationCode, handleRequest);
        }

        public void AddClientPeer(MasterPeer masterPeer)
        {
            MasterPeers[masterPeer.NetPeer.Id] = masterPeer;
        }

        public MasterPeer? GetClientPeer(int id)
        {
            if (MasterPeers.ContainsKey(id))
            {
                return MasterPeers[id];
            }
            return null;
        }
    }
}
