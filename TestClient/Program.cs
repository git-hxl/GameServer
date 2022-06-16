using CommonLibrary.MessagePack;
using CommonLibrary.Operations;
using CommonLibrary.Utils;
using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;

namespace TestClient
{
    internal class Program
    {
        private static NetManager? client;
        private static NetPeer? peer;
        static void Main(string[] args)
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            client = new NetManager(listener);
            client.Start();

            listener.NetworkReceiveEvent += (fromPeer, reader, deliveryMethod) =>
            {
                OperationCode operationCode = (OperationCode)reader.GetByte();

                switch (operationCode)
                {
                    case OperationCode.Login:
                        LoginResponse(reader.GetRemainingBytes());
                        break;
                    case OperationCode.JoinLobby:
                        break;
                    case OperationCode.LevelLobby:
                        break;
                    case OperationCode.Disconnect:
                        break;
                    case OperationCode.CreateGame:
                        break;
                    case OperationCode.JoinGame:
                        break;
                    case OperationCode.JoinRandomGame:
                        break;
                    case OperationCode.GetGameList:
                        break;
                    default:
                        break;
                }
                reader.Recycle();
            };

            Task.Run(() =>
            {
                while(true)
                {
                    string? operation = Console.ReadLine();
                    if (!string.IsNullOrEmpty(operation))
                    {
                        if (operation == "Connect")
                        {
                            Connect();
                        }
                        if (operation == "Login")
                        {
                            Login();
                        }
                    }
                }
            });

            while (true)
            {
                try
                {
                    client.PollEvents();
                    Thread.Sleep(15);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }
            }

            client?.Stop();
        }
        static void Connect()
        {
            peer = client?.Connect("192.168.0.104" /* host ip or name */, 8000 /* port */, "Hello" /* text key or NetDataWriter */);
        }

        static void Login()
        {
            LoginRequest request = new LoginRequest();
            request.TimeStamp = DateTimeEx.TimeStamp();
            request.Account = "xxoo";
            request.Password = "123456";
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationCode.Login);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));
            peer?.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        static void LoginResponse(byte[] bytes)
        {
            LoginResponse loginResponse = MessagePackSerializer.Deserialize<LoginResponse>(bytes);

            Console.WriteLine("{0}: LoginResult :{1}", loginResponse.ID, loginResponse.ReturnCode.ToString());
        }
    }
}