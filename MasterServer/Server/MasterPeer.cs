using LiteNetLib;
using MasterServer.Game;
using MessagePack;
using Serilog;
using SharedLibrary.Message;
using SharedLibrary.Operation;
using SharedLibrary.Server;
using SharedLibrary.Utils;

namespace MasterServer.Server
{
    internal class MasterPeer : ServerPeer
    {
        public MasterPeer(NetPeer peer) : base(peer)
        {

        }

        public async Task RegisterRequest(byte[] data)
        {
            RegisterRequest request = MessagePackSerializer.Deserialize<RegisterRequest>(data);

            //todo:注册信息验证
            string sql = $"select uid from user where account='{request.Account}'";
            var selectResult = await MySQLTool.QueryAsync<string>(sql);
            if (selectResult != null && selectResult.Count > 0)
            {
                Log.Information("register failed");
                SendResponseToClient(OperationCode.Register, ReturnCode.RegisterFailedByAccountIsExisted, null, DeliveryMethod.ReliableOrdered);
                return;
            }

            string uid = Guid.NewGuid().ToString();

            sql = $"insert into user(uid,account,password,nickname) values('{uid}','{request.Account}','{request.Password}','{request.UserInfo.NickName}'";

            int insertResult = await MySQLTool.ExecuteAsync(sql);

            if (insertResult > 0)
            {
                Log.Information("register success uid:{0}", uid);

                RegisterResponse response = new RegisterResponse();
                response.UID = uid;

                data = MessagePackSerializer.Serialize(response);

                SendResponseToClient(OperationCode.Register, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Log.Information("register failed");

                SendResponseToClient(OperationCode.Register, ReturnCode.RegisterFailed, null, DeliveryMethod.ReliableOrdered);
            }
        }

        public async Task LoginRequest(byte[] data)
        {
            LoginRequest request = MessagePackSerializer.Deserialize<LoginRequest>(data);

            string sql = $"select * from user where account='{request.Account}' && password='{request.Password}'";

            UserInfo = await MySQLTool.QueryFirstOrDefaultAsync<UserInfo>(sql);
            if (UserInfo != null)
            {
                Log.Information("login success uid:{0}", UserInfo.UID);

                LoginResponse response = new LoginResponse();
                response.UserInfo = UserInfo;

                data = MessagePackSerializer.Serialize(response);
                SendResponseToClient(OperationCode.Login, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Log.Information("login failed account:{0}", request.Account);
                SendResponseToClient(OperationCode.Login, ReturnCode.LoginFailed, null, DeliveryMethod.ReliableOrdered);
            }
        }

        public async Task GetRoomListRequest(byte[] data)
        {
            GetRoomResponse response = new GetRoomResponse();
            response.RoomInfos = MasterServer.Instance.RoomInfos;

            data = MessagePackSerializer.Serialize(response);
            SendResponseToClient(OperationCode.GetRoomList, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);
        }

        public void CreateRoomRequest(byte[] data)
        {
            CreateRoomRequest request = MessagePackSerializer.Deserialize<CreateRoomRequest>(data);

            RoomInfo roomInfo = request.RoomInfo;

            roomInfo.RoomID = Guid.NewGuid().ToString();

            GamePeer gamePeer = MasterServer.Instance.GetLowerLoadLevelingServer();

            data = MessagePackSerializer.Serialize(request);

            gamePeer.SendRequestToServer(ServerOperationCode.CreateRoom, data, DeliveryMethod.ReliableOrdered);


            CreateRoomResponse response = new CreateRoomResponse();
            response.RoomID = roomInfo.RoomID;
            response.GameServer = gamePeer.NetPeer.EndPoint.ToString();
            data = MessagePackSerializer.Serialize(response);
            SendResponseToClient(OperationCode.CreateRoom, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);
        }

    }
}