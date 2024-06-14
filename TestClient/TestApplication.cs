using LiteNetLib.Utils;
using LiteNetLib;
using Serilog;
using MessagePack;

using SharedLibrary;
using System.Net;
using Newtonsoft.Json;
using System.Text;

namespace TestClient
{
    internal class TestApplication
    {
        protected static NetManager netManager;
        protected static EventBasedNetListener eventBasedNetListener;

        protected static NetPeer ServerNetPeer { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            eventBasedNetListener = new EventBasedNetListener();

            eventBasedNetListener.ConnectionRequestEvent += OnConnectionRequest;
            eventBasedNetListener.PeerConnectedEvent += OnPeerConnected;
            eventBasedNetListener.PeerDisconnectedEvent += OnPeerDisconnected;
            eventBasedNetListener.NetworkReceiveEvent += OnNetworkReceive;

            netManager = new NetManager(eventBasedNetListener);
            netManager.ChannelsCount = 4;

            netManager.Start();

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration.WriteTo.File("./Log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).WriteTo.Console();
            loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();



            Console.WriteLine(netManager.LocalPort);

            while (true)
            {
                netManager.PollEvents();
                Thread.Sleep(15);


                string command = "";

                if (isauto == false)
                {
                    command = Console.ReadLine();
                }

                try
                {
                    if (command.Contains("connect"))
                    {
                        string[] commands = command.Split(" ");

                        ServerNetPeer = netManager.Connect("127.0.0.1", int.Parse(commands[1]), "qwer123456");
                    }

                    if (command.Contains("register"))
                    {
                        string[] commands = command.Split(" ");
                        RegisterTest(commands[1], commands[2]);
                    }

                    if (command.Contains("login"))
                    {
                        string[] commands = command.Split(" ");
                        LoginTest(commands[1], commands[2]);
                    }

                    if (command.Contains("createroom"))
                    {
                        string[] commands = command.Split(" ");
                        CreateRoomTest(commands[1]);
                    }

                    if (command.Contains("joinroom"))
                    {
                        string[] commands = command.Split(" ");
                        JoinRoomTest(commands[1]);
                    }

                    if (command.Contains("exitroom"))
                    {
                        string[] commands = command.Split(" ");
                        ExitRoomTest();
                    }

                    if (command.Contains("getroomlist"))
                    {
                        GetRoomListTest();
                    }

                    if (command.Contains("auto"))
                    {
                        AuotSync();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

        }

        protected static void OnConnectionRequest(ConnectionRequest request)
        {
            if (netManager.ConnectedPeersCount < 999)
                request.AcceptIfKey("");
            else
                request.Reject();
        }

        protected static void OnPeerConnected(NetPeer peer)
        {
            Log.Information("peer connection: {0}", peer.EndPoint);
        }

        protected static void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Information("peer disconnection: {0} info: {1}", peer.EndPoint, disconnectInfo.Reason.ToString());
        }

        protected static void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                OperationCode operationCode = (OperationCode)reader.GetUShort();
                ReturnCode returnCode;
                switch (channel)
                {
                    case 0:
                        Log.Information("操作代码 {0}", operationCode);

                        if (operationCode == OperationCode.SyncEvent)
                        {
                            byte[] data = reader.GetRemainingBytes();

                            string msg = Encoding.UTF8.GetString(data);

                            Log.Information("操作代码 {0} msg {1}", operationCode, msg);
                        }
                        //OperationDefaultHandler.OnRequest(operationCode, MasterPeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;
                    case 1:

                        returnCode = (ReturnCode)reader.GetUShort();

                        Log.Information("操作代码 {0} 返回代码 {1}", operationCode, returnCode);

                        Console.WriteLine(operationCode.ToString() + " " + returnCode.ToString());

                        if (operationCode == OperationCode.CreateRoom && returnCode == ReturnCode.Success)
                        {
                            CreateRoomResponse response = MessagePackSerializer.Deserialize<CreateRoomResponse>(reader.GetRemainingBytes());

                            IPEndPoint iPEndPoint = IPEndPoint.Parse(response.GameServer);

                            Console.WriteLine(response.GameServer);
                        }
                        if (operationCode == OperationCode.GetRoomList && returnCode == ReturnCode.Success)
                        {
                            List<RoomInfo> response = MessagePackSerializer.Deserialize<List<RoomInfo>>(reader.GetRemainingBytes());

                            Console.WriteLine(JsonConvert.SerializeObject(response));
                        }
                        //OperationDefaultHandler.OnResponse(operationCode, returnCode, MasterPeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error("peer receive error: {0}", ex.Message);
            }
        }


        private static void RegisterTest(string arg1, string arg2, string arg3 = "", string arg4 = "")
        {
            RegisterRequest request = new RegisterRequest();
            request.Account = arg1;
            request.Password = arg2;
            //request.UserInfo = new UserInfo();
            //request.UserInfo.NickName= "Test";

            SendRequest(OperationCode.Register, MessagePackSerializer.Serialize(request), DeliveryMethod.ReliableOrdered);
        }

        private static void LoginTest(string arg1, string arg2)
        {
            LoginRequest request = new LoginRequest();
            request.Account = arg1;
            request.Password = arg2;
            SendRequest(OperationCode.Login, MessagePackSerializer.Serialize(request), DeliveryMethod.ReliableOrdered);
        }

        private static void CreateRoomTest(string arg1)
        {
            CreateRoomRequest request = new CreateRoomRequest();
            request.RoomName = arg1;
            request.RoomType = 1;
            request.RoomMaxPlayers = 6;
            request.RoomPassword = "";
            SendRequest(OperationCode.CreateRoom, MessagePackSerializer.Serialize(request), DeliveryMethod.ReliableOrdered);
        }

        private static void JoinRoomTest(string arg1)
        {
            JoinRoomRequest request = new JoinRoomRequest();
            request.RoomID = arg1;
            request.UserInfo = new UserInfo() { NickName = "aaaa", UID = "qwe123" };
            SendRequest(OperationCode.JoinRoom, MessagePackSerializer.Serialize(request), DeliveryMethod.ReliableOrdered);
        }

        private static void ExitRoomTest()
        {
            SendRequest(OperationCode.LeaveRoom, null, DeliveryMethod.ReliableOrdered);
        }


        private static void GetRoomListTest()
        {
            SendRequest(OperationCode.GetRoomList, null, DeliveryMethod.ReliableOrdered);
        }

        public static void SendRequest(OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((short)operationCode);
            if (data != null)
                netDataWriter.Put(data);
            ServerNetPeer.Send(netDataWriter, 0, deliveryMethod);
        }

        public static void SendResponse(OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((short)operationCode);
            netDataWriter.Put((short)returnCode);
            if (data != null)
                netDataWriter.Put(data);
            ServerNetPeer.Send(netDataWriter, 1, deliveryMethod);
        }

        static bool isauto;
        public static void AuotSync()
        {
            isauto = true;
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(30);

                    NetDataWriter netDataWriter = new NetDataWriter();
                    netDataWriter.Put((short)OperationCode.SyncEvent);
                    Random random = new Random();
                    string msg = random.Next(0, 100) + "_" + random.Next(10000, 10000000);
                    netDataWriter.Put(Encoding.UTF8.GetBytes(msg));
                    ServerNetPeer.Send(netDataWriter, 0, DeliveryMethod.ReliableSequenced);
                }
            });
        }
    }
}