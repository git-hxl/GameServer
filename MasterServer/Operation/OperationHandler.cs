using LiteNetLib;
using Serilog;
using Dapper;
using MessagePack;
using SharedLibrary;

namespace MasterServer
{
    public class OperationHandler : OperationHandlerBase
    {
        public override void OnRequest(BasePeer basePeer, OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            //Log.Information("操作代码 {0}", operationCode);

            switch (operationCode)
            {
                case OperationCode.UpdateGameServerInfo:
                    break;
                case OperationCode.UpdateRoomList:
                    break;
                case OperationCode.Register:
                    OnRegister(basePeer, data);
                    break;
                case OperationCode.Login:
                    OnLogin(basePeer, data);
                    break;
                case OperationCode.GetRoomList:
                    OnGetRoomList(basePeer);
                    break;
                case OperationCode.CreateRoom:
                    OnCreateRoom(basePeer, data);
                    break;
                case OperationCode.JoinRoom:
                    break;
                case OperationCode.LeaveRoom:
                    break;
                default:
                    Log.Error("未知的操作代码 {0}", operationCode);
                    break;
            }
        }
        public override void OnResponse(BasePeer basePeer, OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            Log.Information("操作代码 {0} 返回代码 {1}", operationCode, returnCode);
        }

        private async void OnRegister(BasePeer basePeer, byte[] data)
        {
            RegisterRequest request = MessagePackSerializer.Deserialize<RegisterRequest>(data);

            var sqlCon = await MySqlManager.Instance.GetConnection();

            //todo:注册信息验证
            string sql = $"select uid from user where account='{request.Account}'";
            var selectResult = await sqlCon.QueryAsync<string>(sql);
            if (selectResult != null && selectResult.Count() > 0)
            {
                Log.Information("register failed：账号已存在");
                basePeer.SendResponse(OperationCode.Register, ReturnCode.RegisterFailedByAccountIsExisted, null, DeliveryMethod.ReliableOrdered);
                return;
            }

            sql = $"insert into user(account,password) values('{request.Account}','{request.Password}')";

            int insertResult = await sqlCon.ExecuteAsync(sql);

            if (insertResult > 0)
            {
                int uid = await sqlCon.QueryFirstAsync<int>("select LAST_INSERT_ID()");
                Log.Information("register success uid:{0}", uid);

                RegisterResponse response = new RegisterResponse();
                response.UID = uid;
                data = MessagePackSerializer.Serialize(response);
                basePeer.SendResponse(OperationCode.Register, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Log.Information("register failed：sql 执行失败");
                basePeer.SendResponse(OperationCode.Register, ReturnCode.RegisterFailed, null, DeliveryMethod.ReliableOrdered);
            }
            sqlCon.Close();
        }

        private async void OnLogin(BasePeer basePeer, byte[] data)
        {
            LoginRequest request = MessagePackSerializer.Deserialize<LoginRequest>(data);

            string sql = $"select * from user where account='{request.Account}' && password='{request.Password}'";

            UserInfo userInfo = await MySqlManager.Instance.QueryFirstOrDefaultAsync<UserInfo>(sql);
            if (userInfo != null)
            {
                Log.Information("login success uid:{0}", userInfo.UID);

                LoginResponse response = new LoginResponse();
                response.UserInfo = userInfo;

                data = MessagePackSerializer.Serialize(response);
                basePeer.SendResponse(OperationCode.Login, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Log.Information("login failed account:{0}", request.Account);
                basePeer.SendResponse(OperationCode.Login, ReturnCode.LoginFailed, null, DeliveryMethod.ReliableOrdered);
            }
        }

        private void OnGetRoomList(BasePeer basePeer)
        {
            var servers = MasterServer.Instance.GamePeers;

            List<RoomInfo> roomInfos = new List<RoomInfo>();

            for (int i = 0; i < servers.Count; i++)
            {
                roomInfos.AddRange(servers[i].Rooms);
            }

            byte[] data = MessagePackSerializer.Serialize(roomInfos);

            basePeer.SendResponse(OperationCode.GetRoomList, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);
        }

        private void OnCreateRoom(BasePeer basePeer, byte[] data)
        {
            CreateRoomRequest request = MessagePackSerializer.Deserialize<CreateRoomRequest>(data);

            RoomInfo roomInfo = new RoomInfo();

            roomInfo.RoomID = Guid.NewGuid().ToString();

            roomInfo.RoomName = request.RoomName;
            roomInfo.RoomType = request.RoomType;
            roomInfo.RoomDescription = request.RoomDescription;
            roomInfo.RoomPassword = request.RoomPassword;
            roomInfo.RoomMaxPlayers = request.RoomMaxPlayers;

            GamePeer? gamePeer = MasterServer.Instance.GetLowerLoadLevelingServer();

            //没有合适的服务器
            if (gamePeer == null)
            {
                basePeer.SendResponse(OperationCode.CreateRoom, ReturnCode.CreateRoomFailed, null, DeliveryMethod.ReliableOrdered);

                return;
            }

            data = MessagePackSerializer.Serialize(roomInfo);

            //向Game服务器请求创建房间
            gamePeer.SendRequest(OperationCode.CreateRoom, data, DeliveryMethod.ReliableOrdered);

            CreateRoomResponse response = new CreateRoomResponse();
            response.RoomID = roomInfo.RoomID;
            response.GameServer = gamePeer.NetPeer.EndPoint.ToString();
            data = MessagePackSerializer.Serialize(response);

            //回复Client请求结果
            basePeer.SendResponse(OperationCode.CreateRoom, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);
        }
    }
}
