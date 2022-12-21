
using LiteNetLib;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
using SharedLibrary.Server;
namespace TestClient
{
    public class TestServer
    {
        public NetManager client;
        private EventBasedNetListener listener;

        public NetPeer MasterPeer;
        public TestServer()
        {
            listener = new EventBasedNetListener();

            listener.NetworkReceiveEvent += OnNetworkReceive;
            listener.PeerConnectedEvent += OnPeerConnected;
            listener.PeerDisconnectedEvent += OnPeerDisconnected;
            client = new NetManager(listener);
        }

        private void OnPeerConnected(NetPeer peer)
        {
            MasterPeer = peer;
            Log.Information("peer connection: {0}", peer.EndPoint);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Information("peer disconnection: {0} info: {1}", peer.EndPoint, disconnectInfo.Reason.ToString());
        }


        public void Start()
        {
            client.Start(8888);

            client.Connect("127.0.0.1", 6000, "yoyo");
        }

        public void Update()
        {
            client.PollEvents();
        }

        private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
               // OperationCode2 operationCode = (OperationCode2)reader.GetByte();

            }
            catch (Exception ex)
            {
                Log.Error("peer receive error: {0}", ex.Message);
            }
        }

        public void Stop()
        {
            client.Stop();
        }
    }
}