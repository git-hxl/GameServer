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

            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;

            listener.NetworkReceiveEvent += (fromPeer, reader, deliveryMethod) =>
            {
                OperationCode operationCode = (OperationCode)reader.GetByte();

                switch (operationCode)
                {
                    case OperationCode.Register:
                        RegisterResponse(reader.GetRemainingBytes());
                        break;
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
                while (true)
                {
                    string? operation = Console.ReadLine();
                    if (!string.IsNullOrEmpty(operation))
                    {
                        if (operation.Contains("Connect"))
                        {
                            Connect();
                        }
                        if (operation.Contains("Register"))
                        {
                            string[] msg = operation.Split(" ");
                            Register(msg[1], msg[2]);
                        }

                        if (operation.Contains("Login"))
                        {
                            string[] msg = operation.Split(" ");
                            Login(msg[1], msg[2]);
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
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }
            }
            Console.ReadKey();
            client?.Stop();
        }

        private static void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Console.WriteLine("Connect to server:" + peer.Id);
        }

        static void Connect()
        {
            peer = client?.Connect("192.168.0.104" /* host ip or name */, 8000 /* port */, "Hello" /* text key or NetDataWriter */);
        }

        static void Register(string account, string password)
        {
            RegisterRequestPack request = new RegisterRequestPack();
            request.Account = account;
            request.Password = password;
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationCode.Register);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));
            peer?.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        static void Login(string account, string password)
        {
            LoginRequestPack request = new LoginRequestPack();
            request.Account = account;
            request.Password = password;
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationCode.Login);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));
            peer?.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        static void RegisterResponse(byte[] bytes)
        {
            LoginResponsePack response = MessagePackSerializer.Deserialize<LoginResponsePack>(bytes);
            Console.WriteLine("{0}: RegisterResult:{1} SendTime {2} Ping:{3}", response.ID,response.ReturnCode.ToString(), response.TimeStamp, DateTimeEx.TimeStamp - response.TimeStamp);
        }

        static void LoginResponse(byte[] bytes)
        {
            LoginResponsePack response = MessagePackSerializer.Deserialize<LoginResponsePack>(bytes);
            Console.WriteLine("{0}: LoginResult:{1} SendTime {2} Ping:{3}", response.ID, response.ReturnCode.ToString(), response.TimeStamp, DateTimeEx.TimeStamp - response.TimeStamp);
        }


    }
}