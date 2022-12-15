using LiteNetLib;
using MasterServer.MySQL;
using MasterServer.Operation;
using Newtonsoft.Json;
using Serilog;
namespace MasterServer
{
    internal class MasterServer
    {
        private NetManager server;
        private EventBasedNetListener listener;
        private OperationHandler handler;
        public MasterConfig MasterConfig { get; private set; }

        private List<MasterPeer> masterPeers = new List<MasterPeer>();
        public MasterServer()
        {
            listener = new EventBasedNetListener();
            listener.ConnectionRequestEvent += OnConnectionRequestEvent;
            listener.PeerConnectedEvent += OnPeerConnected;
            listener.PeerDisconnectedEvent += OnPeerDisconnected;
            listener.NetworkReceiveEvent += OnNetworkReceive;

            handler = new OperationHandler();

            MasterConfig = JsonConvert.DeserializeObject<MasterConfig>(File.ReadAllText("./MasterConfig.json"));

            MySQLTool.SQLConnectionStr = MasterConfig.SQLConnectionStr;

            server = new NetManager(listener);

            server.PingInterval = MasterConfig.PingInterval;
            server.DisconnectTimeout = MasterConfig.DisconnectTimeout;
            server.ReconnectDelay = MasterConfig.ReconnectDelay;
            server.MaxConnectAttempts = MasterConfig.MaxConnectAttempts;
        }

        public void Start()
        {
            server.Start(MasterConfig.Port);
            Log.Information("start server:{0}", server.LocalPort);
        }

        public void Update()
        {
            server.PollEvents();
        }

        private void OnConnectionRequestEvent(ConnectionRequest request)
        {
            if (server.ConnectedPeersCount < MasterConfig.MaxPeers)
                request.AcceptIfKey(MasterConfig.ConnectKey);
            else
                request.Reject();
        }

        private void OnPeerConnected(NetPeer peer)
        {
            masterPeers.Add(new MasterPeer(peer));
            Log.Information("peer connection: {0}", peer.EndPoint);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            masterPeers.Remove(masterPeers.First((a) => a.NetPeer == peer));
            Log.Information("peer disconnection: {0} info: {1}", peer.EndPoint, disconnectInfo.Reason.ToString());
        }

        private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                OperationCode operationCode = (OperationCode)reader.GetByte();
                MasterPeer masterPeer = masterPeers.First((a) => a.NetPeer == peer);
                handler.OnOperationRequest(operationCode, masterPeer, reader.GetRemainingBytes(), deliveryMethod);
            }
            catch (Exception ex)
            {
                Log.Error("peer receive error: {0}", ex.Message);
            }
        }

        public void Stop()
        {
            server.Stop();
        }
    }
}