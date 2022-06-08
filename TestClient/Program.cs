using LiteNetLib;
using LiteNetLib.Utils;
using System.Net.Sockets;
using System.Text;

namespace TestClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager client = new NetManager(listener);
            client.Start();
            client.Connect("localhost" /* host ip or name */, 8000 /* port */, "Hello" /* text key or NetDataWriter */);
            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                Console.WriteLine("We got: {0} {1}", dataReader.GetString(100 /* max length of string */),dataReader.Position);
                dataReader.Recycle();
            };

            while (!Console.KeyAvailable)
            {
                client.PollEvents();
                NetDataWriter writer = new NetDataWriter();
                writer.Put(Encoding.UTF8.GetBytes("Haha"));
                client.FirstPeer.Send(writer, DeliveryMethod.ReliableOrdered);
                Thread.Sleep(15);
            }

            client.Stop();
        }
    }
}