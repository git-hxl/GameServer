using CommonLibrary.MessagePack;
using LiteNetLib;
using LiteNetLib.Utils;
namespace TestClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager client = new NetManager(listener);
            client.Start();
            client.ChannelsCount = 3;
            NetPeer peer = client.Connect("192.168.0.104" /* host ip or name */, 8000 /* port */, "Hello" /* text key or NetDataWriter */);
            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                Console.WriteLine("We got: {0} {1}", dataReader.GetString(100 /* max length of string */), dataReader.Position);
                dataReader.Recycle();
            };
            int id = 0;

            while (!Console.KeyAvailable)
            {
                client.PollEvents();
                id++;
                TestPack testPack = new TestPack()
                {
                    dateTime = DateTime.UtcNow.ToString(),
                    id = id,
                };
                NetDataWriter writer = new NetDataWriter();
                writer.Put(MessagePack.MessagePackSerializer.Serialize(testPack));
                Random random = new Random();
                byte channel = (byte)random.Next(3);
                peer.Send(writer, channel, DeliveryMethod.ReliableOrdered);
                Thread.Sleep(15);
            }

            client.Stop();
        }
    }
}