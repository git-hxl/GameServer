using CommonLibrary.MessagePack;
using CommonLibrary.Operations;
using CommonLibrary.Utils;
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

            while (!Console.KeyAvailable)
            {
                string? operation = Console.ReadLine();
                if(!string.IsNullOrEmpty(operation))
                {
                    if(operation == "Login")
                    {
                        Login(peer);
                    }
                }
                client.PollEvents();
                Thread.Sleep(15);
            }

            client.Stop();
        }

        static void Login(NetPeer peer)
        {
            LoginRequest request = new LoginRequest();
            request.TimeStamp = DateTimeEx.TimeStamp();
            request.Account = "xxoo";
            request.Password = "123456";
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationCode.Login);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));

            peer.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }
    }
}