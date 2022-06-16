using CommonLibrary;
using CommonLibrary.MessagePack;
using CommonLibrary.Operations;
using CommonLibrary.Utils;
using Dapper;
using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using MySqlConnector;
namespace MasterServer
{
    internal class MasterApplication : Singleton<MasterApplication>
    {
        private NetManager? server;
        private EventBasedNetListener? listener;
        private string dbConnectStr = "server=localhost;user=root;password=123456;database=database";
        public void Init()
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.Start(MasterServerSetting.Port);
            server.PingInterval = 1000;
            server.DisconnectTimeout = 5000;
            server.ReconnectDelay = 500;
            //最大连接尝试次数
            server.MaxConnectAttempts = 10;

            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
        }

        public void Update()
        {
            if (server != null)
            {
                server.PollEvents();
            }
        }

        private void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (server != null && server.ConnectedPeersCount < 10)
                request.AcceptIfKey(MasterServerSetting.ConnectKey);
            else
                request.Reject();
        }

        private void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Console.WriteLine("We got connection: {0} id:{1}", peer.EndPoint, peer.Id);
        }

        private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine("We got disconnection: {0}", peer.EndPoint);
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            OperationCode operationCode = (OperationCode)reader.GetByte();

            switch (operationCode)
            {
                case OperationCode.Login:
                    HandleLoginRequest(peer, reader.GetRemainingBytes());
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
        }


        private async void HandleLoginRequest(NetPeer netPeer, byte[] requestData)
        {
            LoginRequest loginRequest = MessagePackSerializer.Deserialize<LoginRequest>(requestData);
            using (var connection = new MySqlConnection(dbConnectStr))
            {
                try
                {
                    await connection.OpenAsync();
                    string sql = $"select * from user where account='{loginRequest.Account}'&&password='{loginRequest.Password}'";
                    using (var command = new MySqlCommand(sql, connection))
                    {
                        var reader = await command.ExecuteReaderAsync();
                        var users = reader.Parse<UserTable>().ToList();
                        LoginResponse loginResponse = new LoginResponse();
                        if (users.Count() > 0)
                        {
                            foreach (var item in users)
                            {
                                Console.WriteLine("id {0} account {1} password {2}", item.ID, item.Account, item.Password);
                                loginResponse.ID = item.ID;
                            }
                            loginResponse.ReturnCode = ReturnCode.Success;
                        }
                        else
                        {
                            loginResponse.ReturnCode = ReturnCode.Failed;
                        }
                        SendResponse(netPeer, loginResponse);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }

        }

        private void SendResponse(NetPeer netPeer, LoginResponse responseBase)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)OperationCode.Login);
            netDataWriter.Put(MessagePack.MessagePackSerializer.Serialize(responseBase));
            netPeer.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        private void AllocatePeerToDefaultLobby(NetPeer peer)
        {

        }
    }
}
