using LiteNetLib.Utils;
using LiteNetLib;
using Serilog;
using MessagePack;
using System.Diagnostics;
using MasterServer.Operation;
using SharedLibrary.Operation;
using SharedLibrary.Message;
using System.Net;

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

            ServerNetPeer = netManager.Connect("127.0.0.1", 6666, "qwer123456");

            while (true)
            {
                netManager.PollEvents();
                Thread.Sleep(15);

                string command = Console.ReadLine();

                try
                {
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
                        CreateRoomTest(commands[1], commands[2]);
                    }

                    if (command.Contains("joinroom"))
                    {
                        string[] commands = command.Split(" ");
                        JoinRoomTest(commands[1], commands[2]);
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
                OperationCode operationCode = (OperationCode)reader.GetByte();
                ReturnCode returnCode;
                switch (channel)
                {
                    case 2:
                        //OperationDefaultHandler.OnRequest(operationCode, MasterPeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;
                    case 3:
                        returnCode = (ReturnCode)reader.GetByte();

                        Console.WriteLine(operationCode.ToString() + " " + returnCode.ToString());

                        if(operationCode == OperationCode.CreateRoom&& returnCode == ReturnCode.Success)
                        {
                            CreateRoomResponse response = MessagePackSerializer.Deserialize<CreateRoomResponse>(reader.GetRemainingBytes());

                            IPEndPoint iPEndPoint = IPEndPoint.Parse(response.GameServer);

                            Console.WriteLine(response.GameServer);

                            ServerNetPeer = netManager.Connect(iPEndPoint, "qwer123456");
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
            request.UserInfo = new UserInfo();
            request.UserInfo.NickName= "Test";

            SendRequest(OperationCode.Register, MessagePackSerializer.Serialize(request), DeliveryMethod.ReliableOrdered);
        }

        private static void LoginTest(string arg1, string arg2)
        {
            LoginRequest request = new LoginRequest();
            request.Account = arg1;
            request.Password = arg2;
            SendRequest(OperationCode.Login, MessagePackSerializer.Serialize(request), DeliveryMethod.ReliableOrdered);
        }

        private static void CreateRoomTest(string arg1, string arg2)
        {
            CreateRoomRequest request = new CreateRoomRequest();
            request.RoomInfo = new RoomInfo();
            request.RoomInfo.RoomName = arg1;
            request.RoomInfo.RoomType = 1;
            request.RoomInfo.RoomMaxPlayers = 6;
            request.RoomInfo.RoomPassword = arg2;
            SendRequest(OperationCode.CreateRoom, MessagePackSerializer.Serialize(request), DeliveryMethod.ReliableOrdered);
        }

        private static void JoinRoomTest(string arg1, string arg2)
        {
            JoinRoomRequest request = new JoinRoomRequest();
            request.RoomID = arg1;
            request.RoomPassword = arg2;
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
            netDataWriter.Put((byte)operationCode);
            if (data != null)
                netDataWriter.Put(data);
            ServerNetPeer.Send(netDataWriter, 2, deliveryMethod);
        }

        public static void SendResponse(OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)operationCode);
            netDataWriter.Put((byte)returnCode);
            if (data != null)
                netDataWriter.Put(data);
            ServerNetPeer.Send(netDataWriter, 3, deliveryMethod);
        }
    }
}