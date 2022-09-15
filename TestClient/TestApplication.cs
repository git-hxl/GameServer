using GameServer.Client;
using GameServer.Request;
using LiteNetLib;
using LiteNetLib.Utils;
using Newtonsoft.Json;
using Serilog;
using SharedLibrary.Operation;
using SharedLibrary.Server;

namespace TestClient
{
    internal class TestApplication
    {
        private static NetPeer netPeer;
        private static ServerConfig serverConfig;
        static void Main(string[] args)
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration.WriteTo.Console();
            loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();

            serverConfig = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText("./ServerConfig.json"));

            TestServer gameServer = new TestServer(serverConfig);

            gameServer.Start();

            netPeer = gameServer.Connect("127.0.0.1", 6000, serverConfig.connectKey);

            while (true)
            {
                gameServer.Update();
                Thread.Sleep(15);

                string command = Console.ReadLine();

                if (command.Contains("send"))
                {
                    string[] commands = command.Split(" ");
                    if (commands.Length >= 3)
                    {
                        Send(commands[1], int.Parse(commands[2]));
                    }
                }
                if (command.Contains("auth"))
                {
                    Auth();
                }
                if (command.Contains("connect"))
                {
                    Connect();
                }
                if (command.Contains("create room"))
                {
                    CreateRoom();
                }
                if (command.Contains("join"))
                {
                    string[] commands = command.Split(" ");
                    if (commands.Length >= 3)
                    {
                        JoinRoom(commands[1], commands[2]);
                    }
                }
                if (command.Contains("leave"))
                {
                    LeaveRoom();
                }

                if (command.Contains("get roomlist"))
                {
                    GetRoomList();
                }
            }
        }

        public static void Send(string txt, int type)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put(txt);

            netPeer.Send(netDataWriter, (DeliveryMethod)(type));
        }

        public static void Connect()
        {
            netPeer = netPeer.NetManager.Connect("218.75.44.6", 6000, "yoyo");
        }

        public static void Auth()
        {
            AuthRequest authRequest = new AuthRequest();
            Token token = new Token();
            token.UserID = "1000";
            token.NickName = "hxl";

            string json = JsonConvert.SerializeObject(token);

            string encyptJson = SharedLibrary.Utils.SecurityUtil.AESEncrypt(json, serverConfig.encryptKey);

            authRequest.Token = encyptJson;

            byte[] data = MessagePack.MessagePackSerializer.Serialize(authRequest);
            OperationRequest operationRequest = new OperationRequest(OperationCode.Auth, data, DeliveryMethod.ReliableOrdered);
            operationRequest.SendTo(netPeer);
        }

        public static void CreateRoom()
        {
            CreateRoomRequest request = new CreateRoomRequest();
            request.MaxPeers = 10;
            request.RoomName = "hxl house";
            request.Password = "123456";

            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            OperationRequest operationRequest = new OperationRequest(OperationCode.CreateRoom, data, DeliveryMethod.ReliableOrdered);
            operationRequest.SendTo(netPeer);
        }

        public static void JoinRoom(string roomID,string password)
        {
            JoinRoomRequest request = new JoinRoomRequest();
            request.RoomID = roomID;
            request.Password = password;

            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            OperationRequest operationRequest = new OperationRequest(OperationCode.JoinRoom, data, DeliveryMethod.ReliableOrdered);
            operationRequest.SendTo(netPeer);
        }

        public static void LeaveRoom()
        {
            LeaveRoomRequest request = new LeaveRoomRequest();
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            OperationRequest operationRequest = new OperationRequest(OperationCode.LeaveRoom, data, DeliveryMethod.ReliableOrdered);
            operationRequest.SendTo(netPeer);
        }

        public static void GetRoomList()
        {
            GetRoomListRequest request = new GetRoomListRequest();
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            OperationRequest operationRequest = new OperationRequest(OperationCode.GetRoomList, data, DeliveryMethod.ReliableOrdered);
            operationRequest.SendTo(netPeer);
        }
    }
}