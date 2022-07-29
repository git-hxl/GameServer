using CommonLibrary.Core;
using CommonLibrary.MessagePack;
using LiteNetLib;
using MasterServer.Operations;
using Newtonsoft.Json;
using Serilog;

namespace MasterServer
{
    public class MasterApplication : ApplicationBase
    {
        private Dictionary<int, MasterPeer> clientPeers = new Dictionary<int, MasterPeer>();
        public MasterServerConfig? ServerConfig { get; private set; }
        public static MasterApplication Instance = new MasterApplication();
        public MasterApplication() : base(new MasterOperationHandle())
        { }

        protected override void InitConfig()
        {
            string config = File.ReadAllText("./MasterServerConfig.json");
            if (!string.IsNullOrEmpty(config))
                ServerConfig = JsonConvert.DeserializeObject<MasterServerConfig>(config);

            if (ServerConfig == null)
            {
                Log.Error("No Config Loaded!");
                return;
            }

            server.PingInterval = ServerConfig.PingInterval;
            server.DisconnectTimeout = ServerConfig.DisconnectTimeout;
            server.ReconnectDelay = ServerConfig.ReconnectDelay;
            server.MaxConnectAttempts = ServerConfig.MaxConnectAttempts;

            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;

            Log.Information("Start Master Server");
        }

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

            if (clientPeers.ContainsKey(peer.Id))
            {
                clientPeers[peer.Id].OnDisConnected();
                clientPeers.Remove(peer.Id);
            }
        }

        protected override void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            MsgPack msgPack = MessagePack.MessagePackSerializer.Deserialize<MsgPack>(reader.GetRemainingBytes());
            operationHandle.HandleRequest(peer, msgPack, deliveryMethod);
        }

        public void AddClientPeer(MasterPeer masterPeer)
        {
            clientPeers[masterPeer.NetPeer.Id] = masterPeer;
        }

        public MasterPeer? GetClientPeer(int id)
        {
            if (clientPeers.ContainsKey(id))
            {
                return clientPeers[id];
            }
            return null;
        }
    }
}
