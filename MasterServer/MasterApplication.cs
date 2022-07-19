using CommonLibrary.Operations;
using CommonLibrary.Utils;
using LiteNetLib;
using MasterServer.Operations;
using Newtonsoft.Json;
using Serilog;

namespace MasterServer
{
    internal class MasterApplication : Singleton<MasterApplication>
    {
        private NetManager server;
        private EventBasedNetListener listener;
        private OperationHandleBase operationHandle;
        private Dictionary<string, MasterPeer> clientPeers = new Dictionary<string, MasterPeer>();
        public MasterServerConfig? ServerConfig { get; }
        public MasterApplication()
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            operationHandle = new OperationHandleBase();
            Log.Information("Load Config");
            try
            {
                string config = File.ReadAllText("./MasterServerConfig.json");
                if (!string.IsNullOrEmpty(config))
                    ServerConfig = JsonConvert.DeserializeObject<MasterServerConfig>(config);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            };
        }

        public void Start()
        {
            if (ServerConfig == null)
            {
                Log.Error("No Config Loaded!");
                return;
            }
            server.Start(ServerConfig.Port);
            server.PingInterval = 1000;
            server.DisconnectTimeout = 5000;
            server.ReconnectDelay = 500;
            //最大连接尝试次数
            server.MaxConnectAttempts = 10;

            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;

            Log.Information("Start listener Successed");
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

        private void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (server != null && ServerConfig != null && server.ConnectedPeersCount < 5000)
                request.AcceptIfKey(ServerConfig.ConnectKey);
            else
            {
                request.Reject();
                Log.Information("Reject Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }

        private void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Log.Information("We got connection: {0} id:{1}", peer.EndPoint, peer.Id);
            MasterPeer clientPeer = new MasterPeer(peer);
            clientPeers[peer.EndPoint.ToString()] = clientPeer;
        }

        private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Information("We got disconnection: {0}", peer.EndPoint);

            if (clientPeers.ContainsKey(peer.EndPoint.ToString()))
            {
                clientPeers[peer.EndPoint.ToString()].OnDisConnected();
                clientPeers.Remove(peer.EndPoint.ToString());
            }
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (clientPeers.ContainsKey(peer.EndPoint.ToString()))
            {
                MasterPeer clientPeer = clientPeers[peer.EndPoint.ToString()];
                OperationCode operationCode = (OperationCode)reader.GetByte();
                HandleRequest handleRequest = new HandleRequest(clientPeer, operationCode, reader.GetRemainingBytes());
                operationHandle.HandleRequest(handleRequest);
            }
            else
            {
                Log.Information("Client {0} not connected!", peer.EndPoint.ToString());
            }
        }
    }
}
