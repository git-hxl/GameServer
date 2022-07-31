using CommonLibrary.Core;
using Dapper;
using LiteNetLib;
using MasterServer.DB;
using MasterServer.DB.Table;
using MasterServer.Operations.Request;
using MasterServer.Operations.Response;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

namespace MasterServer.Operations
{
    public class MasterOperationHandle
    {
        public void HandleRequest(OperationCode operationCode, HandleRequest handleRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            switch (operationCode)
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
                case OperationCode.LevelLobby:
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
                    HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.InvalidRequest, null, handleRequest.DeliveryMethod);
                    break;
            }
            Console.WriteLine("{0}：{1} 代码耗时：{2}", DateTime.Now.ToLongTimeString(), operationCode.ToString(), stopwatch.ElapsedMilliseconds);
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
                        sql = $"insert into user (ID,Account,Password,NickName,RealName,Identify,LastLoginTime) values " +
                           $"({0},'{request.Account}','{request.Password}','{request.NickName}','{request.RealName}','{request.Identity}','{lastlogintime}')";

                        int result = dbConnection.Execute(sql);
                        if (result == 1)
                        {
                            HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnRegister, null, handleRequest.DeliveryMethod);
                            return;
                        }
                    }
                }
            }
            HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnRegisterFailed, null, handleRequest.DeliveryMethod);
        }

        private async void LoginRequest(HandleRequest handleRequest)
        {
            LoginRequest request = MessagePackSerializer.Deserialize<LoginRequest>(handleRequest.MsgPack.Data);
            using (var dbConnection = await DBHelper.CreateConnection())
            {
                if (dbConnection != null)
                {
                    string sql = $"select * from user where account='{request.Account}'&&password='{request.Password}'";
                    UserTable user = dbConnection.QueryFirstOrDefault<UserTable>(sql);
                    if (user != null)
                    {
                        sql = $"update user set lastlogintime='{DateTime.Now}' where account='{request.Account}'";
                        dbConnection.Execute(sql);
                        MasterPeer masterPeer = new MasterPeer(handleRequest.Peer, user);
                        MasterApplication.Instance.AddClientPeer(masterPeer);
                        OnLoginResponse response = new OnLoginResponse();
                        response.UserTable = user;
                        response.Lobbies = LobbyFactory.Instance.Lobbies.Select((a)=>a.LobbyProperty).ToList();
                        byte[] data = MessagePackSerializer.Serialize(response);
                        MsgPack msgPack = new MsgPack(data);
                        HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnLogin, msgPack, handleRequest.DeliveryMethod);
                        return;
                    }
                }
            }
            HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnLoginFailed, null, handleRequest.DeliveryMethod);
        }

        private void JoinLobbyRequest(HandleRequest handleRequest)
        {
            JoinLobbyRequest request = MessagePackSerializer.Deserialize<JoinLobbyRequest>(handleRequest.MsgPack.Data);
            if (!string.IsNullOrEmpty(request.LobbyID))
            {
                Lobby lobby = LobbyFactory.Instance.GetOrCreateLobby();
                MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.Peer.Id);
                if (masterPeer != null && !masterPeer.IsInLooby)
                {
                    lobby.AddClientPeer(masterPeer);
                    masterPeer.OnJoinLobby(lobby);
                    OnJoinLobbyResponse response = new OnJoinLobbyResponse();
                    response.LobbyProperty = lobby.LobbyProperty;
                    response.Rooms = lobby.Rooms.Select((a) => a.RoomProperty).ToList();
                    byte[] data = MessagePackSerializer.Serialize(response);
                    MsgPack msgPack = new MsgPack(data);
                    HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnJoinLobby, msgPack, handleRequest.DeliveryMethod);
                }
            }
            HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnJoinLobbyFailed, null, handleRequest.DeliveryMethod);
        }

        private void GetRoomListRequest(HandleRequest handleRequest)
        {
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.Peer.Id);
            if (masterPeer != null && masterPeer.IsInLooby)
            {
                if (masterPeer.CurLobby != null)
                {
                    OnRoomListResponse response = new OnRoomListResponse();
                    response.LobbyProperty = masterPeer.CurLobby.LobbyProperty;
                    response.Rooms = masterPeer.CurLobby.Rooms.Select((a) => a.RoomProperty).ToList();
                    byte[] data = MessagePackSerializer.Serialize(response);
                    MsgPack msgPack = new MsgPack(data);
                    HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnRoomListUpdate, msgPack, handleRequest.DeliveryMethod);
                }
            }
            HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.InvalidRequest, null, handleRequest.DeliveryMethod);
        }

        private void LeaveLobbyRequest(HandleRequest handleRequest)
        {
            LeaveLobbyRequest request = MessagePackSerializer.Deserialize<LeaveLobbyRequest>(handleRequest.MsgPack.Data);
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.Peer.Id);
            if (masterPeer != null && masterPeer.IsInLooby)
            {
                masterPeer.CurLobby?.RemoveClientPeer(masterPeer);
                masterPeer.OnLeaveLobby();
                HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnLeaveLobby, null, handleRequest.DeliveryMethod);
                return;
            }
            HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnLeaveLobbyFailed, null, handleRequest.DeliveryMethod);
        }

        private void CreateRoomRequest(HandleRequest handleRequest)
        {
            CreateRoomRequest request = MessagePackSerializer.Deserialize<CreateRoomRequest>(handleRequest.MsgPack.Data);
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.Peer.Id);
            if (masterPeer != null && masterPeer.IsInLooby)
            {
                Room? room = masterPeer.CurLobby?.CreateRoom(masterPeer, request.RoomName, request.IsVisible, request.NeedPassword, request.Password, request.MaxPlayers, request.CustomProperties);
                if (room != null)
                {
                    OnJoinRoomResponse response = new OnJoinRoomResponse();
                    response.RoomProperty = room.RoomProperty;
                    response.Users = room.ClientPeers.Select((a) => a.User).ToList();
                    byte[] data = MessagePackSerializer.Serialize(response);
                    MsgPack msgPack = new MsgPack(data);
                    HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnCreateRoom, msgPack, handleRequest.DeliveryMethod);
                    return;
                }
            }
            HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnCreateRoomFailed, null, handleRequest.DeliveryMethod);
        }


        private void JoinRoomRequest(HandleRequest handleRequest)
        {
            JoinRoomRequest request = MessagePackSerializer.Deserialize<JoinRoomRequest>(handleRequest.MsgPack.Data);
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.Peer.Id);
            if (masterPeer != null && !masterPeer.IsInRoom && masterPeer.IsInLooby)
            {
                Room? room = masterPeer.CurLobby?.GetRoom(request.RoomID);
                if (room != null)
                {
                    OnJoinRoomResponse response = new OnJoinRoomResponse();
                    response.RoomProperty = room.RoomProperty;
                    response.Users = room.ClientPeers.Select((a) => a.User).ToList();
                    byte[] data = MessagePackSerializer.Serialize(response);
                    MsgPack msgPack = new MsgPack(data);
                    HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnJoinRoom, msgPack, handleRequest.DeliveryMethod);
                    //call others
                    foreach (var item in room.ClientPeers)
                    {
                        if (item != masterPeer)
                            HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnPlayerJoinRoom, msgPack, handleRequest.DeliveryMethod);
                    }
                    return;
                }
            }
            HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnJoinRoomFailed, null, handleRequest.DeliveryMethod);
        }

        private void LeaveRoomRequest(HandleRequest handleRequest)
        {
            LeaveRoomRequest request = MessagePackSerializer.Deserialize<LeaveRoomRequest>(handleRequest.MsgPack.Data);
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.Peer.Id);
            if (masterPeer != null && masterPeer.IsInRoom)
            {
                Room? room = masterPeer.CurRoom;
                room?.RemoveClientPeer(masterPeer);
                masterPeer.OnLeaveRoom();
                HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnLeaveRoom, null, handleRequest.DeliveryMethod);
                if (room != null)
                {
                    OnJoinRoomResponse response = new OnJoinRoomResponse();
                    response.RoomProperty = room.RoomProperty;
                    response.Users = room.ClientPeers.Select((a) => a.User).ToList();
                    byte[] data = MessagePackSerializer.Serialize(response);
                    MsgPack msgPack = new MsgPack(data);
                    //call others
                    foreach (var item in room.ClientPeers)
                    {
                        HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnPlayerLeaveRoom, msgPack, handleRequest.DeliveryMethod);
                    }
                }
                return;
            }
            HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnLeaveRoomFailed, null, handleRequest.DeliveryMethod);
        }
    }
}