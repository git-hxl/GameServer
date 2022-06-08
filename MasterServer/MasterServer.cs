using LiteNetLib;
using LiteNetLib.Utils;

namespace MasterServer
{
    internal class MasterServer
    {
        private NetManager server;
        private EventBasedNetListener listener;
        public NetManager Server { get { return server; } }
        public void Init()
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.Start(8000);
            listener.ConnectionRequestEvent += request =>
            {
                if (server.ConnectedPeersCount < 10 /* max connections */)
                    request.AcceptIfKey("Hello");
                else
                    request.Reject();
            };
            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("We got connection: {0}", peer.EndPoint); // Show peer ip
            };

            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
        }

        /// <summary>
        /// 接受消息
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="reader"></param>
        /// <param name="deliveryMethod"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            Console.WriteLine("We got: {0} {1}", reader.GetString(100 /* max length of string */), reader.Position);
            reader.Recycle();
        }

        /// <summary>
        /// 断开链接
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="disconnectInfo"></param>
        private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine("{0} id:{1} disconnected,issue:{2}", peer.EndPoint.ToString(), peer.Id, disconnectInfo.Reason.ToString());
        }

        public void Update()
        {
            server.PollEvents();
        }

        public void SendAll(byte[] data)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put(data);
            server.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        }


    }
}
