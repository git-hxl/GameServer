using System.Net.Sockets;
using System.Net;

namespace GameServer
{
    sealed class SocketService
    {
        public UdpClient Socket { get; private set; }

        public bool IsConnected { get; private set; }

        private List<IPEndPoint> remoteClints = new List<IPEndPoint>();

        public void StartRecive(int port)
        {
            Socket = new UdpClient(port);
            Task.Run(async () =>
            {
                while (true)
                {
                    UdpReceiveResult udpReceiveResult = await Socket.ReceiveAsync();
                    if (!remoteClints.Contains(udpReceiveResult.RemoteEndPoint))
                        remoteClints.Add(udpReceiveResult.RemoteEndPoint);
                    MsgManager.Instance.Enqueue(udpReceiveResult.Buffer);
                }
            });
            Console.WriteLine("Server Run");
        }

        public void Send(byte[] bytes)
        {
            foreach (var item in remoteClints)
            {
                Socket.SendAsync(bytes, bytes.Length, item);
            }
        }
    }
}