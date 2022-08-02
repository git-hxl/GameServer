using CommonLibrary.Core;
using CommonLibrary.Utils;
using Dapper;
using MasterServer.DB;
using MasterServer.DB.Table;
using MasterServer.Operations.Request;
using MessagePack;
using System.Diagnostics;
namespace MasterServer.Operations
{
    public class MasterOperationHandle
    {
        public void HandleRequest(HandleRequest handleRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            switch (handleRequest.OperationCode)
            {
                case OperationCode.Register:
                    RegisterRequest(handleRequest);
                    break;
                case OperationCode.Login:
                    LoginRequest(handleRequest);
                    break;
                case OperationCode.JoinLobby:
                    JoinLobbyRequest(handleRequest);
                    break;
                case OperationCode.LeaveLobby:
                    LeaveLobbyRequest(handleRequest);
                    break;
                case OperationCode.CreateRoom:
                    CreateRoomRequest(handleRequest);
                    break;
                case OperationCode.JoinRoom:
                    JoinRoomRequest(handleRequest);
                    break;
                case OperationCode.LeaveRoom:
                    LeaveRoomRequest(handleRequest);
                    break;
                case OperationCode.GetRoomList:
                    GetRoomListRequest(handleRequest);
                    break;
                default:
                    HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.InvalidRequest));
                    break;
            }
            Console.WriteLine("{0}：{1} 代码耗时：{2}", DateTime.Now.ToLongTimeString(), handleRequest.OperationCode.ToString(), stopwatch.ElapsedMilliseconds);
        }

        private async void RegisterRequest(HandleRequest handleRequest)
        {
            RegisterRequest request = MessagePackSerializer.Deserialize<RegisterRequest>(handleRequest.MsgPack.Data);
            using (var dbConnection = await DBHelper.CreateConnection())
            {
                if (dbConnection != null)
                {
                    string sql = $"select * from user where account='{request.Account}'";
                    var existAccount = dbConnection.QueryFirstOrDefault<UserTable>(sql);
                    if (existAccount == null)
                    {
                        DateTime lastlogintime = DateTime.Now;
                        request.Password = SecurityUtil.MD5Encrypt(request.Password);
                        sql = $"insert into user (ID,Account,Password,NickName,RealName,Identity,LastLoginTime) values " +
                           $"({0},'{request.Account}','{request.Password}','{request.NickName}','{request.RealName}','{request.Identity}','{lastlogintime}')";

                        int result = dbConnection.Execute(sql);
                        if (result == 1)
                        {
                            RegisterResponse response = new RegisterResponse();
                            response.Account = request.Account;
                            byte[] data = MessagePackSerializer.Serialize(response);
                            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(data));
                            return;
                        }
                    }
                }
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.OnRegisterFailed));
        }

        private async void LoginRequest(HandleRequest handleRequest)
        {
            LoginRequest request = MessagePackSerializer.Deserialize<LoginRequest>(handleRequest.MsgPack.Data);
            if (MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer) == null)
            {
                using (var dbConnection = await DBHelper.CreateConnection())
                {
                    if (dbConnection != null)
                    {
                        request.Password = SecurityUtil.MD5Encrypt(request.Password);
                        string sql = $"select * from user where account='{request.Account}'&&password='{request.Password}'";
                        UserTable user = dbConnection.QueryFirstOrDefault<UserTable>(sql);
                        if (user != null)
                        {
                            sql = $"update user set lastlogintime='{DateTime.Now}' where account='{request.Account}'";
                            dbConnection.Execute(sql);
                            MasterPeer masterPeer = new MasterPeer(handleRequest.NetPeer, user);
                            MasterApplication.Instance.AddClientPeer(handleRequest.NetPeer, masterPeer);
                            LoginResponse response = new LoginResponse();
                            response.User = user;
                            response.Lobbies = LobbyFactory.Instance.Lobbies.Select((a) => a.LobbyProperty).ToList();
                            byte[] data = MessagePackSerializer.Serialize(response);
                            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(data));
                            return;
                        }
                    }
                }
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.OnLoginFailed));
        }

        private void JoinLobbyRequest(HandleRequest handleRequest)
        {
            JoinLobbyRequest request = MessagePackSerializer.Deserialize<JoinLobbyRequest>(handleRequest.MsgPack.Data);
            Lobby lobby = LobbyFactory.Instance.GetOrCreateLobby();
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer);
            if (masterPeer != null && !masterPeer.IsInLooby)
            {
                lobby.AddClientPeer(masterPeer);
                masterPeer.OnJoinLobby(lobby);
                JoinLobbyResponse response = new JoinLobbyResponse();
                response.UserID = masterPeer.User.ID;
                response.LobbyID = lobby.LobbyProperty.LobbyID;
                response.Rooms = lobby.Rooms.Select((a) => a.RoomProperty).ToList();
                byte[] data = MessagePackSerializer.Serialize(response);
                HandleResponse.SendResponse(handleRequest, MsgPack.Pack(data));
                return;
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.OnJoinLobbyFailed));
        }

        private void GetRoomListRequest(HandleRequest handleRequest)
        {
            GetRoomListRequest request = MessagePackSerializer.Deserialize<GetRoomListRequest>(handleRequest.MsgPack.Data);
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer);
            if (masterPeer != null && masterPeer.IsInLooby)
            {
                if (masterPeer.CurLobby != null)
                {
                    GetRoomListResponse response = new GetRoomListResponse();
                    response.Rooms = masterPeer.CurLobby.Rooms.Select((a) => a.RoomProperty).ToList();
                    byte[] data = MessagePackSerializer.Serialize(response);
                    HandleResponse.SendResponse(handleRequest, MsgPack.Pack(data));
                    return;
                }
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.InvalidRequest));
        }

        private void LeaveLobbyRequest(HandleRequest handleRequest)
        {
            LeaveLobbyRequest request = MessagePackSerializer.Deserialize<LeaveLobbyRequest>(handleRequest.MsgPack.Data);
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer);
            if (masterPeer != null && masterPeer.IsInLooby)
            {
                Lobby lobby = masterPeer.CurLobby;
                if (lobby != null)
                {
                    lobby.RemoveClientPeer(masterPeer);
                    masterPeer.OnLeaveLobby();

                    LeaveLobbyResponse response = new LeaveLobbyResponse();
                    response.UserID = masterPeer.User.ID;
                    response.LobbyID = lobby.LobbyProperty.LobbyID;
                    byte[] data = MessagePackSerializer.Serialize(response);
                    HandleResponse.SendResponse(handleRequest, MsgPack.Pack(data));
                    return;
                }
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.OnLeaveLobbyFailed));
        }

        private void CreateRoomRequest(HandleRequest handleRequest)
        {
            CreateRoomRequest request = MessagePackSerializer.Deserialize<CreateRoomRequest>(handleRequest.MsgPack.Data);
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer);
            if (masterPeer != null && masterPeer.IsInLooby)
            {
                Room? room = masterPeer.CurLobby?.CreateRoom(masterPeer, request.RoomName, request.IsVisible, request.NeedPassword, request.Password, request.MaxPlayers, request.CustomProperties);
                if (room != null)
                {
                    CreateRoomResponse response = new CreateRoomResponse();
                    response.RoomProperty = room.RoomProperty;
                    response.Users = room.MasterPeers.Select((a) => a.User).ToList();
                    byte[] data = MessagePackSerializer.Serialize(response);
                    HandleResponse.SendResponse(handleRequest, MsgPack.Pack(data));
                    return;
                }
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.OnCreateGameFailed));
        }


        private void JoinRoomRequest(HandleRequest handleRequest)
        {
            JoinRoomRequest request = MessagePackSerializer.Deserialize<JoinRoomRequest>(handleRequest.MsgPack.Data);
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer);
            if (masterPeer != null && !masterPeer.IsInRoom && masterPeer.IsInLooby)
            {
                Room? room = masterPeer.CurLobby?.GetRoom(request.RoomID);
                if (room != null)
                {
                    room.AddClientPeer(masterPeer);
                    masterPeer.OnJoinRoom(room);

                    JoinRoomResponse response = new JoinRoomResponse();
                    response.UserID = masterPeer.User.ID;
                    response.RoomID = room.RoomProperty.RoomID;
                    response.RoomProperty = room.RoomProperty;
                    response.Users = room.MasterPeers.Select((a) => a.User).ToList();
                    byte[] data = MessagePackSerializer.Serialize(response);
                    foreach (var item in room.MasterPeers)
                    {
                        HandleResponse.SendToPeer(item.NetPeer, handleRequest.OperationCode, MsgPack.Pack(data));
                    }

                    return;
                }
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.OnJoinRoomFailed));
        }

        private void LeaveRoomRequest(HandleRequest handleRequest)
        {
            LeaveRoomRequest request = MessagePackSerializer.Deserialize<LeaveRoomRequest>(handleRequest.MsgPack.Data);
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer);
            if (masterPeer != null && masterPeer.IsInRoom)
            {
                Room? room = masterPeer.CurRoom;
                if (room != null)
                {
                    LeaveRoomResponse response = new LeaveRoomResponse();
                    response.UserID = masterPeer.User.ID;
                    response.RoomID = room.RoomProperty.RoomID;

                    byte[] data = MessagePackSerializer.Serialize(response);
                    foreach (var item in room.MasterPeers)
                    {
                        HandleResponse.SendToPeer(item.NetPeer, handleRequest.OperationCode, MsgPack.Pack(data));
                    }

                    room.RemoveClientPeer(masterPeer);
                    masterPeer.OnLeaveRoom();
                }
                return;
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.OnLeaveRoomFailed));
        }
    }
}