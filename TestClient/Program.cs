using CommonLibrary.MessagePack;
using CommonLibrary.MessagePack.Operation;
using CommonLibrary.MessagePack.Response;
using CommonLibrary.Utils;
using GameServer.Operations;
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
            //client.UnsyncedEvents = true;
            client.Start();

            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;

            listener.NetworkReceiveEvent += (fromPeer, reader, deliveryMethod) =>
            {
                GameOperationCode operationCode = (GameOperationCode)reader.GetByte();
                ReturnCode returnCode = (ReturnCode)reader.GetByte();

                Console.WriteLine("{0} request result: {1} ping{2}", operationCode.ToString(), returnCode.ToString(), fromPeer.Ping);

                if (returnCode != ReturnCode.Success)
                    return;
                switch (operationCode)
                {
                    case GameOperationCode.JoinGame:
                        RPCResponse(reader.GetRemainingBytes());
                        break;
                    case GameOperationCode.ExitGame:
                        RPCResponse(reader.GetRemainingBytes());
                        break;
                    case GameOperationCode.RPC:
                        RPCResponse(reader.GetRemainingBytes());
                        break;
                }

                //switch (operationCode)
                //{
                //    case OperationCode.Register:
                //        RegisterResponse(reader.GetRemainingBytes());
                //        break;
                //    case OperationCode.Login:
                //        LoginResponse(reader.GetRemainingBytes());
                //        break;
                //    case OperationCode.JoinLobby:

                //        break;
                //    case OperationCode.LevelLobby:
                //        break;
                //    case OperationCode.Disconnect:
                //        break;
                //    case OperationCode.CreateRoom:
                //        CreateRoom(reader.GetRemainingBytes());
                //        break;
                //    case OperationCode.JoinRoom:
                //        CreateRoom(reader.GetRemainingBytes());
                //        break;
                //    case OperationCode.LeaveRoom:
                //        break;
                //    default:
                //        break;
                //}
                reader.Recycle();
            };

            Connect("127.0.0.1", 8001);

            Task.Run(() =>
            {
                while (true)
                {
                    string? operation = Console.ReadLine();
                    if (!string.IsNullOrEmpty(operation))
                    {
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

                        if (operation.Contains("JoinLobby"))
                        {
                            string[] msg = operation.Split(" ");
                            JoinLobby(msg[1]);
                        }

                        if (operation.Contains("CreateRoom"))
                        {
                            string[] msg = operation.Split(" ");
                            CreateRoom(msg[1]);
                        }

                        if (operation.Contains("JoinRoom"))
                        {
                            string[] msg = operation.Split(" ");
                            JoinRoom(msg[1]);
                        }

                        if (operation.Contains("LeaveRoom"))
                        {
                            LeaveRoom();
                        }

                        if (operation.Contains("JoinGame"))
                        {
                            JoinGame();
                        }

                        if (operation.Contains("ExitGame"))
                        {
                            ExitGame();
                        }

                        if (operation.Contains("Rpc"))
                        {
                            Task.Run(() =>
                            {
                                while (true)
                                {
                                    SendRpc();
                                    Thread.Sleep(1);
                                }
                            });
                        }
                    }
                }
            });

            while (true)
            {
                try
                {
                    client.PollEvents();
                    Thread.Sleep(1);
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

        static void Connect(string ip, int port)
        {
            peer = client?.Connect(ip, port, "Hello");
        }

        static void Register(string account, string password)
        {
            RegisterRequest request = new RegisterRequest();
            request.Account = account;
            request.Password = password;
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationCode.Register);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));
            peer?.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        static void Login(string account, string password)
        {
            LoginRequest request = new LoginRequest();
            request.Account = account;
            request.Password = password;
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationCode.Login);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));
            peer?.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }
        static void JoinLobby(string lobbyName)
        {
            JoinLobbyRequestPack request = new JoinLobbyRequestPack();
            request.LobbyName = lobbyName;
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationCode.JoinLobby);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));
            peer?.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }
        static void CreateRoom(string rommName)
        {
            RequestCreateRoom request = new RequestCreateRoom();
            request.RoomName = rommName;
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationCode.CreateRoom);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));
            peer?.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        static void JoinRoom(string roomid)
        {
            JoinRoomRequestPack request = new JoinRoomRequestPack();
            request.RoomID = roomid;
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationCode.JoinRoom);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));
            peer?.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        static void LeaveRoom()
        {
            RequsetBasePack request = new RequsetBasePack();
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationCode.LeaveRoom);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));
            peer?.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        static void RegisterResponse(byte[] bytes)
        {
            ResponsePack response = MessagePackSerializer.Deserialize<ResponsePack>(bytes);
            Console.WriteLine("注册成功 {0} ", DateTimeEx.ConvertToLocalDateTime(response.TimeStamp));
        }

        static void LoginResponse(byte[] bytes)
        {
            LoginResponsePack response = MessagePackSerializer.Deserialize<LoginResponsePack>(bytes);
            Console.WriteLine("登录成功 id:{0} ", response.ID);
        }

        static void CreateRoom(byte[] bytes)
        {
            JoinRoomResponsePack response = MessagePackSerializer.Deserialize<JoinRoomResponsePack>(bytes);
            Console.WriteLine("加入房间 id:{0} ", response.RoomID);
        }

        static int userid = Random.Shared.Next(0, 1000000);
        static void JoinGame()
        {
            RpcPack request = new RpcPack();
            request.RoomID = "asd";
            request.UserID = userid;
            userid = request.UserID;
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)GameOperationCode.JoinGame);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));
            peer?.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        static void ExitGame()
        {
            RpcPack request = new RpcPack();
            request.RoomID = "asd";
            request.UserID = userid;
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)GameOperationCode.ExitGame);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));
            peer?.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        static int sendindex = 0;
        static void SendRpc()
        {
            RpcPack request = new RpcPack();
            request.RoomID = "asd";
            request.UserID = userid;
            request.Param.Add(0, sendindex++);
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)GameOperationCode.RPC);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(request));
            peer?.Send(netDataWriter, DeliveryMethod.ReliableSequenced);
            client.TriggerUpdate();
        }

        static void RPCResponse(byte[] bytes)
        {
            RpcPack response = MessagePackSerializer.Deserialize<RpcPack>(bytes);
            Console.WriteLine("消息Index{0}, 延迟:{1}", response.Param[0], DateTimeEx.TimeStamp - response.TimeStamp);
        }
    }
}