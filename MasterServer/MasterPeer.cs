using LiteNetLib;
using MasterServer.MySQL;
using MasterServer.MySQL.Table;
using MasterServer.Operation.Request;
using MessagePack;
using Serilog;
namespace MasterServer
{
    internal class MasterPeer
    {
        public NetPeer NetPeer { get; private set; }

        public MasterPeer(NetPeer netPeer)
        {
            NetPeer = netPeer;
        }

        public async void RegisterRequest(byte[] data)
        {
            RegisterRequest request = MessagePackSerializer.Deserialize<RegisterRequest>(data);
            string sql = $"insert into user(account,password) values('{request.Account}','{request.Password}')";
            var result = await MySQLTool.ExecuteAsync(sql);
            if (result > 0)
            {
                sql = $"select * from user where account='{request.Account}'";
                var userTable = await MySQLTool.QueryFirstOrDefaultAsync<UserTable>(sql);
                Log.Information("register success id:{0}", userTable.ID);
            }
            else
            {
                Log.Information("register failed");
            }
        }

        public async void LoginRequest(byte[] data)
        {
            LoginRequest request = MessagePackSerializer.Deserialize<LoginRequest>(data);
            string sql = $"select * from user where account='{request.Account}' && password='{request.Password}'";
            var userTable = await MySQLTool.QueryFirstOrDefaultAsync<UserTable>(sql);
            if (userTable != null)
            {
                Log.Information("login success id:{0}", userTable.ID);
            }
        }

        public async void CreateRoomRequest(byte[] data)
        {
            CreateRoomRequest request = MessagePackSerializer.Deserialize<CreateRoomRequest>(data);
            
        }
    }
}