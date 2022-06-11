using CommonLibrary.MessagePack;
using CommonLibrary.Utils;
using LiteNetLib;
namespace MasterServer
{
    internal class MasterApplication : Singleton<MasterApplication>
    {
        private NetManager? server;
        private EventBasedNetListener? listener;
        private Lobby.Lobby? defaultLobby;
        public void Init()
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.ChannelsCount = 3;
            server.Start(8000);
            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
            defaultLobby = new Lobby.Lobby("Default", 0, -1);
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
            if (server != null && server.ConnectedPeersCount < 10)
                request.AcceptIfKey("Hello");
            else
                request.Reject();
        }

        private void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Console.WriteLine("We got connection: {0} id:{1}", peer.EndPoint, peer.Id);

            AllocatePeerToDefaultLobby(peer);
        }

        private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine("We got disconnection: {0}", peer.EndPoint);
            defaultLobby?.OnLeaveLobby(peer);
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            TestPack testPack = MessagePack.MessagePackSerializer.Deserialize<TestPack>(reader.GetRemainingBytes());
            
            Console.WriteLine(MessagePack.MessagePackSerializer.SerializeToJson(testPack)+" 大小："+reader.UserDataSize);
        }
        

        private void AllocatePeerToDefaultLobby(NetPeer peer)
        {
            defaultLobby?.OnJoinLobby(peer);
        }

    }
}
