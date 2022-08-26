using CommonLibrary.Core;
using CommonLibrary.Utils;
using CoreLibrary.Utils;
using GameServer.Operations;
using LiteNetLib;
using LiteNetLib.Utils;
using MasterServer;
using MasterServer.Operations.Request;
using MessagePack;
using Newtonsoft.Json;
using System.Collections;

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
            client.UnsyncedEvents = true;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;

            listener.NetworkReceiveEvent += (fromPeer, reader, deliveryMethod) =>
            {
                OperationCode operationCode = (OperationCode)reader.GetByte();
                MsgPack msgPack = MessagePack.MessagePackSerializer.Deserialize<MsgPack>(reader.GetRemainingBytes());

                Console.WriteLine("{0} request result: {1} ping：{2} delay：{3}", operationCode.ToString(), msgPack.ReturnCode.ToString(), fromPeer.Ping, DateTimeEx.TimeStamp - msgPack.TimeStamp);

                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(msgPack));

                if (msgPack.ReturnCode != ReturnCode.Success)
                    return;

                switch (operationCode)
                {
                    case OperationCode.GetRoomList:
                        OnGetRoomList(msgPack);
                        break;
                }
                reader.Recycle();
            };

            Connect("127.0.0.1", 8000);

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

                        if (operation.Contains("AutoTest"))
                        {
                            AutoTest();
                        }

                        if (operation.Contains("JoinLobby"))
                        {
                            JoinLobby();
                        }

                        if (operation.Contains("LeaveLobby"))
                        {
                            LeaveLobby();
                        }

                        if (operation.Contains("CreateRoom"))
                        {
                            CreateRoom();
                        }

                        if (operation.Contains("JoinRoom"))
                        {
                            JoinRoom();
                        }

                        if (operation.Contains("LeaveRoom"))
                        {
                            LeaveRoom();
                        }

                        if (operation.Contains("GetRoomList"))
                        {
                            GetRoomList();
                        }

                        if (operation.Contains("UpdateRoom"))
                        {
                            UpdateRoom();
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

        public static void AutoTest()
        {
            string[] methods = new string[] { "JoinLobby", "GetRoomList", "LeaveLobby", "CreateRoom", "JoinRoom", "LeaveRoom", "UpdateRoom" };
            Task.Run(() =>
            {
                while (true)
                {
                   Thread.Sleep(100);

                    string method = methods[Random.Shared.Next(0, methods.Length)];

                    if (method.Contains("JoinLobby"))
                    {
                        JoinLobby();
                    }

                    if (method.Contains("LeaveLobby"))
                    {
                        LeaveLobby();
                    }

                    if (method.Contains("CreateRoom"))
                    {
                        CreateRoom();
                    }

                    if (method.Contains("JoinRoom"))
                    {
                        JoinRoom();
                    }

                    if (method.Contains("LeaveRoom"))
                    {
                        LeaveRoom();
                    }

                    if (method.Contains("GetRoomList"))
                    {
                        GetRoomList();
                    }

                    if (method.Contains("UpdateRoom"))
                    {
                        UpdateRoom();
                    }

                }

            });
            
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
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            HandleResponse.SendToPeer(peer, OperationCode.Register, MsgPack.Pack(data));
        }

        static void Login(string account, string password)
        {
            LoginRequest request = new LoginRequest();
            request.Account = account;
            request.Password = password;
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            HandleResponse.SendToPeer(peer, OperationCode.Login, MsgPack.Pack(data));
        }

        static void JoinLobby()
        {
            HandleResponse.SendToPeer(peer, OperationCode.JoinLobby, MsgPack.Pack(null));
        }

        static void GetRoomList()
        {
            HandleResponse.SendToPeer(peer, OperationCode.GetRoomList, MsgPack.Pack(null));
        }

        public static List<RoomProperty> Rooms = new List<RoomProperty>();
        static void OnGetRoomList(MsgPack msgPack)
        {
            GetRoomListResponse response = MessagePack.MessagePackSerializer.Deserialize<GetRoomListResponse>(msgPack.Data);

            Rooms = response.Rooms;
        }

        static void LeaveLobby()
        {
            HandleResponse.SendToPeer(peer, OperationCode.LeaveLobby, MsgPack.Pack(null));
        }

        static void CreateRoom()
        {
            CreateRoomRequest request = new CreateRoomRequest();
            string[] strs = new string[] { "car", "ship", "air", "bird", "dog", "fish", "beer" };
            request.RoomName = strs[Random.Shared.Next(0, strs.Length)];
            request.MaxPlayers = 6;
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            HandleResponse.SendToPeer(peer, OperationCode.CreateRoom, MsgPack.Pack(data));
        }


        static void JoinRoom()
        {
            if (Rooms.Count <= 0)
                return;
            JoinRoomRequest request = new JoinRoomRequest();
            request.RoomID = Rooms[Random.Shared.Next(0, Rooms.Count)].RoomID;
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            HandleResponse.SendToPeer(peer, OperationCode.JoinRoom, MsgPack.Pack(data));
        }

        static void LeaveRoom()
        {
            HandleResponse.SendToPeer(peer, OperationCode.LeaveRoom, MsgPack.Pack(null));
        }

        static void UpdateRoom()
        {
            UpdateRoomPropertyRequest request = new UpdateRoomPropertyRequest();
            string[] strs = new string[] { "car", "ship", "air", "bird", "dog", "fish", "beer" };
            Hashtable hashtable = new Hashtable();
            hashtable["test"] = strs[Random.Shared.Next(0, strs.Length)];
            request.CustomProperties = hashtable;
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            HandleResponse.SendToPeer(peer, OperationCode.UpdateRoomProperty, MsgPack.Pack(data));
        }
    }
}