using LiteNetLib;
using Serilog;

namespace CommonLibrary.Core
{
    public abstract class ApplicationBase
    {
        protected NetManager server;
        protected EventBasedNetListener listener;
        protected OperationHandleBase operationHandle;

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

        protected abstract void Listener_ConnectionRequestEvent(ConnectionRequest request);
        protected abstract void Listener_PeerConnectedEvent(NetPeer peer);
        protected abstract void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo);
        protected abstract void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod);
    }
}
