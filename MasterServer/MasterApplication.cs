using CommonLibrary.Core;
using LiteNetLib;
using MasterServer.Operations;
using Newtonsoft.Json;
using Serilog;

namespace MasterServer
{
    public class MasterApplication
    {
        public static MasterApplication Instance { get; private set; } = new MasterApplication();
        public NetManager server { get; private set; }
        public EventBasedNetListener listener { get; private set; }
        public MasterServerConfig? ServerConfig { get; private set; }
        public Dictionary<NetPeer, MasterPeer> MasterPeers { get; private set; }
        public MasterOperationHandle MasterOperationHandle { get; private set; }
        public MasterApplication() : base()
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            MasterPeers = new Dictionary<NetPeer, MasterPeer>();
            MasterOperationHandle = new MasterOperationHandle();

            string config = File.ReadAllText("./MasterServerConfig.json");
            if (!string.IsNullOrEmpty(config))
                ServerConfig = JsonConvert.DeserializeObject<MasterServerConfig>(config);
        }

        public void Start()
        {
            if (ServerConfig == null)
            {
                Log.Error("No Config Loaded!");
                return;
            }
            server.PingInterval = ServerConfig.PingInterval;
            server.DisconnectTimeout = ServerConfig.DisconnectTimeout;
            server.ReconnectDelay = ServerConfig.ReconnectDelay;
            server.MaxConnectAttempts = ServerConfig.MaxConnectAttempts;

            server.UnsyncedEvents = true;

            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;

            server.Start(ServerConfig.Port);
            Log.Information("Start Master Server");
        }

        public void Close()
        {
            if (server != null)
                server.Stop(true);
        }

        public void Update()
        {
            if (server != null)
            {
                server.PollEvents();
            }
        }

        protected void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (ServerConfig != null && server.ConnectedPeersCount < ServerConfig.MaxPeers)
                request.AcceptIfKey(ServerConfig.ConnectKey);
            else
            {
                request.Reject();
                Log.Information("Reject Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }

        protected void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Log.Information("We got connection: {0} id:{1}", peer.EndPoint, peer.Id);
        }

        protected void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Information("We got disconnection: {0}", peer.EndPoint);

            if (MasterPeers.ContainsKey(peer))
            {
                MasterPeers[peer].OnDisConnected();
                MasterPeers.Remove(peer);
            }

            Log.Information("peers count: {0}", MasterPeers.Count);
        }

        protected void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            OperationCode operationCode = (OperationCode)reader.GetByte();
            try
            {
                MsgPack msgPack = MessagePack.MessagePackSerializer.Deserialize<MsgPack>(reader.GetRemainingBytes());
                HandleRequest handleRequest = new HandleRequest(peer, operationCode, msgPack, deliveryMethod);
                MasterOperationHandle.HandleRequest(handleRequest);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        public void AddClientPeer(NetPeer netPeer, MasterPeer masterPeer)
        {
            MasterPeers[netPeer] = masterPeer;
            Log.Information("peers count: {0}", MasterPeers.Count);
        }

        public MasterPeer? GetClientPeer(NetPeer netPeer)
        {
            if (MasterPeers.ContainsKey(netPeer))
            {
                return MasterPeers[netPeer];
            }
            return null;
        }
    }
}
