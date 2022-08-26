using CommonLibrary.Core;
using CommonLibrary.Utils;
using CoreLibrary.Utils;
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
                case OperationCode.UpdateRoomProperty:
                    UpdateRoomRequest(handleRequest);
                    break;
                default:
                    HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.InvalidRequest));
                    break;
            }
            Console.WriteLine("{0}：{1} 代码耗时：{2}", DateTime.Now.ToLongTimeString(), handleRequest.OperationCode.ToString(), stopwatch.ElapsedMilliseconds);
        }

        private async void RegisterRequest(HandleRequest handleRequest)
        {
            if (handleRequest.MsgPack.Data != null && handleRequest.MsgPack.Data.Length > 0)
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
                            sql = $"insert into user (id,account,password,nickname,realname,identity,lastlogintime) values " +
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
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.Failed));
        }

        private async void LoginRequest(HandleRequest handleRequest)
        {
            if (handleRequest.MsgPack.Data != null && handleRequest.MsgPack.Data.Length > 0)
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
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.Failed));
        }

        private void JoinLobbyRequest(HandleRequest handleRequest)
        {
            Lobby lobby = LobbyFactory.Instance.GetOrCreateLobby();
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer);
            if (masterPeer != null && !masterPeer.IsInLooby)
            {
                if (lobby.AddClientPeer(masterPeer))
                {
                    masterPeer.OnJoinLobby(lobby);
                    JoinLobbyResponse response = new JoinLobbyResponse();
                    response.LobbyID = lobby.LobbyProperty.LobbyID;
                    response.Rooms = lobby.Rooms.Select((a) => a.RoomProperty).ToList();
                    byte[] data = MessagePackSerializer.Serialize(response);
                    HandleResponse.SendResponse(handleRequest, MsgPack.Pack(data));
                    return;
                }
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.Failed));
        }


        private void LeaveLobbyRequest(HandleRequest handleRequest)
        {
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer);
            if (masterPeer != null && masterPeer.IsInLooby)
            {
                if (masterPeer.CurLobby != null)
                {
                    Lobby lobby = masterPeer.CurLobby;
                    lobby.RemoveClientPeer(masterPeer);
                    masterPeer.OnLeaveLobby();

                    LeaveLobbyResponse response = new LeaveLobbyResponse();
                    response.LobbyID = lobby.LobbyProperty.LobbyID;
                    byte[] data = MessagePackSerializer.Serialize(response);
                    HandleResponse.SendResponse(handleRequest, MsgPack.Pack(data));
                    return;
                }
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.Failed));
        }

        private void CreateRoomRequest(HandleRequest handleRequest)
        {
            if (handleRequest.MsgPack.Data != null && handleRequest.MsgPack.Data.Length > 0)
            {
                CreateRoomRequest request = MessagePackSerializer.Deserialize<CreateRoomRequest>(handleRequest.MsgPack.Data);
                MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer);
                if (masterPeer != null && masterPeer.IsInLooby && !masterPeer.IsInRoom)
                {
                    Room? room = masterPeer.CurLobby?.CreateRoom(masterPeer, request.RoomName, request.IsVisible, request.NeedPassword, request.Password, request.MaxPlayers, request.CustomProperties);
                    if (room != null)
                    {
                        if (room.AddClientPeer(masterPeer, request.Password))
                        {
                            masterPeer.OnJoinRoom(room);

                            CreateRoomResponse response = new CreateRoomResponse();
                            response.RoomProperty = room.RoomProperty;
                            response.Users = room.MasterPeers.Select((a) => a.User).ToList();
                            byte[] data = MessagePackSerializer.Serialize(response);
                            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(data));
                            return;
                        }
                        else
                        {
                            masterPeer.CurLobby?.RemoveRoom(room);
                        }
                    }
                }
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.Failed));
        }


        private void JoinRoomRequest(HandleRequest handleRequest)
        {
            if (handleRequest.MsgPack.Data != null && handleRequest.MsgPack.Data.Length > 0)
            {
                JoinRoomRequest request = MessagePackSerializer.Deserialize<JoinRoomRequest>(handleRequest.MsgPack.Data);
                MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer);
                if (masterPeer != null && !masterPeer.IsInRoom && masterPeer.IsInLooby)
                {
                    Room? room = masterPeer.CurLobby?.GetRoom(request.RoomID);
                    if (room != null)
                    {
                        if (room.AddClientPeer(masterPeer, request.Password))
                        {
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
                }
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.Failed));
        }

        private void LeaveRoomRequest(HandleRequest handleRequest)
        {
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer);
            if (masterPeer != null && masterPeer.IsInRoom)
            {
                Room? room = masterPeer.CurRoom;
                if (room != null)
                {
                    room.RemoveClientPeer(masterPeer);
                    masterPeer.OnLeaveRoom();

                    LeaveRoomResponse response = new LeaveRoomResponse();
                    response.UserID = masterPeer.User.ID;
                    response.RoomID = room.RoomProperty.RoomID;
                    byte[] data = MessagePackSerializer.Serialize(response);
                    HandleResponse.SendToPeer(handleRequest.NetPeer, handleRequest.OperationCode, MsgPack.Pack(data));
                    foreach (var item in room.MasterPeers)
                    {
                        HandleResponse.SendToPeer(item.NetPeer, handleRequest.OperationCode, MsgPack.Pack(data));
                    }
                    return;
                }
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.Failed));
        }

        private void GetRoomListRequest(HandleRequest handleRequest)
        {
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
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.Failed));
        }

        private void UpdateRoomRequest(HandleRequest handleRequest)
        {
            if (handleRequest.MsgPack.Data != null && handleRequest.MsgPack.Data.Length > 0)
            {
                UpdateRoomPropertyRequest request = MessagePackSerializer.Deserialize<UpdateRoomPropertyRequest>(handleRequest.MsgPack.Data);
                MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.NetPeer);
                if (masterPeer != null && masterPeer.IsInRoom)
                {
                    if (masterPeer.CurRoom != null)
                    {
                        foreach (var item in request.CustomProperties.Keys)
                        {
                            masterPeer.CurRoom.RoomProperty.CustomProperties[item] = request.CustomProperties[item];
                        }
                        UpdateRoomPropertyResponse response = new UpdateRoomPropertyResponse();
                        response.RoomID = masterPeer.CurRoom.RoomProperty.RoomID;
                        response.CustomProperties = masterPeer.CurRoom.RoomProperty.CustomProperties;
                        byte[] data = MessagePackSerializer.Serialize(response);
                        HandleResponse.SendResponse(handleRequest, MsgPack.Pack(data));
                        return;
                    }
                }
            }
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.Failed));
        }

    }
}