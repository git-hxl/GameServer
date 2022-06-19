using CommonLibrary.MessagePack;
using CommonLibrary.Operations;
using CommonLibrary.Table;
using LiteNetLib;
using MasterServer.DB;
using MasterServer.Operations;
using MessagePack;
namespace MasterServer
{
    public sealed class ClientPeer
    {
        public int UserID { get; set; }
        public NetPeer NetPeer { get; }
        public bool IsConnected { get; }
        private OperationHandleBase handle;
        public ClientPeer(NetPeer netPeer)
        {
            this.NetPeer = netPeer;
            this.IsConnected = true;
            this.handle = new OperationHandleBase();
        }

        public async Task<HandleResponse?> RegisterRequest(OperationCode operationCode, byte[] requestData)
        {
            RegisterRequestPack registerRequestPack = MessagePackSerializer.Deserialize<RegisterRequestPack>(requestData);
            HandleResponse handleResponse = new HandleResponse(this, operationCode);
            LoginResponsePack loginResponsePack = new LoginResponsePack();
            var dbConnection = await DBHelper.CreateConnection();
            if (dbConnection != null)
            {
                string sql = $"select * from user where account='{registerRequestPack.Account}'";
                var users = await DBHelper.SqlSelect<UserTable>(dbConnection, sql);
                if (users == null || users.Count == 0)
                {
                    string account = registerRequestPack.Account;
                    string password = registerRequestPack.Password;
                    DateTime lastlogintime = DateTime.UtcNow;
                    string sql2 = $"insert into user (id,account,password,lastlogintime) values ({0},'{account}','{password}','{lastlogintime.ToString()}')";
                    int result = await DBHelper.SqlQuery(dbConnection, sql2);
                    if (result == 0)
                    {
                        loginResponsePack.DebugMsg = ErrorMsg.RegisterFailed;
                    }
                    string sql3 = $"select * from user where account='{account}'&&password='{password}'";
                    users = await DBHelper.SqlSelect<UserTable>(dbConnection, sql3);
                    if (users != null && users.Count == 1)
                    {
                        this.UserID = users[0].ID;
                        loginResponsePack.ID = users[0].ID;
                        loginResponsePack.ReturnCode = ReturnCode.Success;
                    }
                    dbConnection.Close();
                }
            }
            else
            {
                loginResponsePack.DebugMsg = ErrorMsg.SqlConnectFailed;
            }
            handleResponse.ResponseData = MessagePackSerializer.Serialize<LoginResponsePack>(loginResponsePack);
            return handleResponse;
        }

        public async Task<HandleResponse?> LoginRequest(OperationCode operationCode, byte[] requestData)
        {
            LoginRequestPack loginRequest = MessagePackSerializer.Deserialize<LoginRequestPack>(requestData);
            HandleResponse handleResponse = new HandleResponse(this, operationCode);
            LoginResponsePack loginResponsePack = new LoginResponsePack();
            var dbConnection = await DBHelper.CreateConnection();
            if (dbConnection != null)
            {
                string sql = $"select * from user where account='{loginRequest.Account}'&&password='{loginRequest.Password}'";
                var users = await DBHelper.SqlSelect<UserTable>(dbConnection, sql);
                if (users != null && users.Count() == 1)
                {
                    loginResponsePack.ID = users[0].ID;
                    loginResponsePack.ReturnCode = ReturnCode.Success;
                }
                else
                {
                    loginResponsePack.DebugMsg = ErrorMsg.LoginFailed;
                }
                dbConnection.Close();
            }
            else
            {
                loginResponsePack.DebugMsg = ErrorMsg.SqlConnectFailed;
            }
            handleResponse.ResponseData = MessagePackSerializer.Serialize<LoginResponsePack>(loginResponsePack);
            return handleResponse;
        }

        public async Task<HandleResponse?> JoinLobbyRequest(OperationCode operationCode, byte[] requestData) {
            
        
        }

        public void LeaveLobbyRequest(byte[] requestData) { }

        public void GetRoomListRequest(byte[] requestData) { }

        public void CreatRoomRequest(byte[] requestData) { }

        public void JoinRoomRequset(byte[] requestData) { }

        private void CreateAuthToken(string id)
        {

        }

    }
}
