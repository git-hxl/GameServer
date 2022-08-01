using CommonLibrary.Core;
using CommonLibrary.Utils;
using GameServer.Operations;
using LiteNetLib;
using LiteNetLib.Utils;
using MasterServer.Operations.Request;
using MasterServer.Operations.Response;
using MessagePack;
using Newtonsoft.Json;

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
                MsgPack msgPack = MessagePack.MessagePackSerializer.Deserialize<MsgPack>(reader.GetRemainingBytes());

                Console.WriteLine("{0} request result: {1} ping：{2} delay：{3}", operationCode.ToString(), msgPack.ReturnCode.ToString(), fromPeer.Ping,DateTimeEx.TimeStamp-msgPack.TimeStamp);

                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(msgPack));

                if (msgPack.ReturnCode != ReturnCode.Success)
                    return;

                switch(operationCode)
                {
                    case OperationCode.Register:
                        OnRegister(msgPack);
                        break;
                    case OperationCode.Login:
                        OnLogin(msgPack);
                        break;
                    case OperationCode.JoinLobby:
                        OnjoinLobby(msgPack);
                        break;
                    case OperationCode.LevelLobby:
                        OnLeaveLobby(msgPack);
                        break;
                    case OperationCode.CreateRoom:
                        OnCreateRoom(msgPack);
                        break;
                    case OperationCode.JoinRoom:
                        OnJoinRoom(msgPack);
                        break;
                    case OperationCode.LeaveRoom:
                        OnLeaveRoom(msgPack);
                        break;
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

                        if (operation.Contains("JoinLobby"))
                        {
                            JoinLobby();
                        }

                        if (operation.Contains("GetRoomList"))
                        {
                            GetRoomList();
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
        static void OnRegister(MsgPack msgPack)
        {
            RegisterResponse response = MessagePack.MessagePackSerializer.Deserialize<RegisterResponse>(msgPack.Data);
            Console.WriteLine(JsonConvert.SerializeObject(response));
        }

        static void Login(string account, string password)
        {
            LoginRequest request = new LoginRequest();
            request.Account = account;
            request.Password = password;
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            HandleResponse.SendToPeer(peer, OperationCode.Login, MsgPack.Pack(data));
        }

        static void OnLogin(MsgPack msgPack)
        {
            LoginResponse response = MessagePack.MessagePackSerializer.Deserialize<LoginResponse>(msgPack.Data);
            Console.WriteLine(JsonConvert.SerializeObject(response));
        }

        static void JoinLobby()
        {
            JoinLobbyRequest request = new JoinLobbyRequest();
            //request.LobbyID = lobbyName;
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            HandleResponse.SendToPeer(peer, OperationCode.JoinLobby, MsgPack.Pack(null));
        }

        static void OnjoinLobby(MsgPack msgPack)
        {
            JoinLobbyResponse response = MessagePack.MessagePackSerializer.Deserialize<JoinLobbyResponse>(msgPack.Data);
            Console.WriteLine(JsonConvert.SerializeObject(response));
        }

        static void GetRoomList()
        {
            GetRoomListRequest request = new GetRoomListRequest();
            //request.LobbyID = lobbyName;
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            HandleResponse.SendToPeer(peer, OperationCode.GetRoomList, MsgPack.Pack(null));
        }

        static void OnGetRoomList(MsgPack msgPack)
        {
            GetRoomListResponse response = MessagePack.MessagePackSerializer.Deserialize<GetRoomListResponse>(msgPack.Data);
            Console.WriteLine(JsonConvert.SerializeObject(response));
        }

        static void LeaveLobby()
        {
            LeaveLobbyRequest request = new LeaveLobbyRequest();
            //request.LobbyID = lobbyName;
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            HandleResponse.SendToPeer(peer, OperationCode.LevelLobby, MsgPack.Pack(null));
        }

        static void OnLeaveLobby(MsgPack msgPack)
        {
            LeaveLobbyResponse response = MessagePack.MessagePackSerializer.Deserialize<LeaveLobbyResponse>(msgPack.Data);
            Console.WriteLine(JsonConvert.SerializeObject(response));
        }


        static void CreateRoom(string rommName)
        {
            CreateRoomRequest request = new CreateRoomRequest();
            request.RoomName = rommName;
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            HandleResponse.SendToPeer(peer, OperationCode.CreateRoom, MsgPack.Pack(data));
        }

        static void OnCreateRoom(MsgPack msgPack)
        {
            CreateRoomResponse response = MessagePack.MessagePackSerializer.Deserialize<CreateRoomResponse>(msgPack.Data);
            Console.WriteLine(JsonConvert.SerializeObject(response));
        }

        static void JoinRoom(string roomid)
        {
            JoinRoomRequest request = new JoinRoomRequest();
            request.RoomID = roomid;
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            HandleResponse.SendToPeer(peer, OperationCode.JoinRoom, MsgPack.Pack(data));
        }

        static void OnJoinRoom(MsgPack msgPack)
        {
            JoinRoomResponse response = MessagePack.MessagePackSerializer.Deserialize<JoinRoomResponse>(msgPack.Data);
            Console.WriteLine(JsonConvert.SerializeObject(response));
        }

        static void LeaveRoom()
        {
            LeaveRoomRequest request = new LeaveRoomRequest();
            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
            HandleResponse.SendToPeer(peer, OperationCode.LeaveGame, MsgPack.Pack(data));
        }

        static void OnLeaveRoom(MsgPack msgPack)
        {
            LeaveRoomResponse response = MessagePack.MessagePackSerializer.Deserialize<LeaveRoomResponse>(msgPack.Data);
            Console.WriteLine(JsonConvert.SerializeObject(response));
        }
    }
}