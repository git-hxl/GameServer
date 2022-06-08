using System.Net;
using System.Net.Sockets;
using GameServer;
namespace GamerServer

{
    public class Program
    {
        private static void Main(string[] args)
        {
            SocketService socketService = new SocketService();
            socketService.StartRecive(8888);

            while (true)
            {
                Thread.Sleep(20);
                MsgManager.Instance.Update();
            }
        }
    }
}

