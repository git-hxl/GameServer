using LiteNetLib;
using MasterServer.Game;
using MasterServer.MySQL;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
using SharedLibrary.Model;
using SharedLibrary.Operation;
using SharedLibrary.Server;

namespace MasterServer
{
    internal class MasterPeer : ServerPeer
    {
        public bool IsGame { get;private set; }
        public MasterPeer(NetPeer peer) : base(peer)
        {
        }

        public async void RegisterGameServer(byte[] data)
        {
            IsGame = true;
            GameServerManager.Instance.RegisterOrUpdate(Peer.EndPoint.ToString(), null);

            Log.Information("server register request:{0}", Peer.EndPoint.ToString());

            SendResponse(OperationCode.GameServerRegister, ReturnCode.Success, null, DeliveryMethod.ReliableOrdered);
        }

        public async void UpdateGameServer(byte[] data)
        {
            if (GameServerManager.Instance.ServerInfos.ContainsKey(Peer.EndPoint.ToString()))
            {
                ServerInfo serverInfo = MessagePackSerializer.Deserialize<ServerInfo>(data);
                GameServerManager.Instance.RegisterOrUpdate(Peer.EndPoint.ToString(), serverInfo);
                Log.Information("server update {0}", JsonConvert.SerializeObject(serverInfo));
            }
        }


        public async void RegisterRequest(byte[] data)
        {
            UserInfo userInfo = MessagePackSerializer.Deserialize<UserInfo>(data);
            string sql = $"insert into user(uid, account,password) values('{Guid.NewGuid().ToString()}','{userInfo.Account}','{userInfo.Password}')";
            var result = await MySQLTool.ExecuteAsync(sql);
            if (result > 0)
            {
                sql = $"select * from user where account='{userInfo.Account}'";
                userInfo = await MySQLTool.QueryFirstOrDefaultAsync<UserInfo>(sql);
                Log.Information("register success uid:{0}", userInfo.UID);

                SendResponse(OperationCode.Register, ReturnCode.Success, null, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Log.Information("register failed");

                SendResponse(OperationCode.Register, ReturnCode.RegisterFailed, null, DeliveryMethod.ReliableOrdered);
            }
        }

        public async void LoginRequest(byte[] data)
        {
            UserInfo userInfo = MessagePackSerializer.Deserialize<UserInfo>(data);

            string sql = $"select * from user where account='{userInfo.Account}' && password='{userInfo.Password}'";
            userInfo = await MySQLTool.QueryFirstOrDefaultAsync<UserInfo>(sql);
            if (userInfo != null)
            {
                Log.Information("login success uid:{0}", userInfo.UID);
                SendResponse(OperationCode.Login, ReturnCode.Success, null, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Log.Information("login failed account:{0}", userInfo.Account);
                SendResponse(OperationCode.Login, ReturnCode.LoginFailed, null, DeliveryMethod.ReliableOrdered);
            }
        }
    }
}