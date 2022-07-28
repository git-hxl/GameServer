using LiteNetLib;
using Serilog;

namespace CommonLibrary.Core
{
    public abstract class ApplicationBase
    {
        private NetManager server;
        private EventBasedNetListener listener;
        private OperationHandleBase operationHandle;

        public ApplicationBase(OperationHandleBase operationHandle)
        {
            this.operationHandle = operationHandle;
            listener = new EventBasedNetListener();
            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent; ;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent; ;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent; ;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent; ;
            server = new NetManager(listener);
        }

        protected abstract void InitConfig();

        public void Start()
        {
            InitConfig();
            server.Start();
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
                HandleRequest handleRequest = new HandleRequest(clientPeer, operationCode, reader.GetRemainingBytes(), deliveryMethod);
                operationHandle.HandleRequest(handleRequest);
            }
            else
            {
                Log.Information("Client {0} not connected!", peer.EndPoint.ToString());
            }
        }
    }
}
