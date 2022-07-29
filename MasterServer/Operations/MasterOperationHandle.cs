using CommonLibrary.Core;
using CommonLibrary.MessagePack;
using CommonLibrary.MessagePack.Operation;
using CommonLibrary.Table;
using Dapper;
using LiteNetLib;
using LiteNetLib.Utils;
using MasterServer.DB;
using MessagePack;
using System.Diagnostics;

namespace MasterServer.Operations
{
    public class MasterOperationHandle : OperationHandleBase
    {
        public override void HandleRequest(NetPeer netPeer, MsgPack msgPack, DeliveryMethod deliveryMethod)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            HandleRequest handleRequest = new HandleRequest(netPeer, msgPack, deliveryMethod);
            switch (msgPack.OperationCode)
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
                case OperationCode.UpdateLobby:
                    UpdateLobbyRequest(handleRequest);
                    break;
                default:
                    SendResponse(netPeer, msgPack, ReturnCode.InvalidRequest, deliveryMethod);
                    break;
            }
            Console.WriteLine("{0}：{1} 代码耗时：{2}", DateTime.Now.ToLongTimeString(), msgPack.OperationCode.ToString(), stopwatch.ElapsedMilliseconds);
        }

        private void SendResponse(NetPeer peer, MsgPack msgPack, ReturnCode returnCode, DeliveryMethod deliveryMethod)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)returnCode);
            byte[] returnData = MessagePackSerializer.Serialize(msgPack);
            netDataWriter.Put(returnData);
            peer.Send(netDataWriter, deliveryMethod);
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
                            SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.Success, handleRequest.DeliveryMethod);
                            return;
                        }
                    }
                }
            }
            SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.RegisterFailed, handleRequest.DeliveryMethod);
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
                        LoginResponse response = new LoginResponse();
                        response.ID = user.ID;
                        response.NickName = user.NickName;
                        handleRequest.MsgPack.Data = MessagePackSerializer.Serialize(response);
                        SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.Success, handleRequest.DeliveryMethod);
                        return;
                    }
                }
            }
            SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.LoginFailed, handleRequest.DeliveryMethod);
        }

        private byte[] GetLobbyData(Lobby lobby)
        {
            JoinLobbyResponse response = new JoinLobbyResponse();
            response.LobbyID = lobby.LobbyID;
            foreach (var room in lobby.Rooms)
            {
                response.Rooms.Add(new CommonLibrary.MessagePack.Room()
                {
                    RoomName = room.RoomName,
                    RoomID = room.RoomID,
                    CurPlayers = room.ClientPeers.Count,
                    MaxPlayers = room.MaxPeers,
                    NeedPassword = room.NeedPassword,
                    RoomProperties = room.RoomProperties,
                });
            }
            return MessagePackSerializer.Serialize(response);
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
                    handleRequest.MsgPack.Data = GetLobbyData(lobby);
                    SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.Success, handleRequest.DeliveryMethod);
                }
            }
            SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.JoinLobbyFailed, handleRequest.DeliveryMethod);
        }

        private void UpdateLobbyRequest(HandleRequest handleRequest)
        {
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.Peer.Id);
            if (masterPeer != null && masterPeer.IsInLooby)
            {
                if (masterPeer.CurLobby != null)
                {
                    handleRequest.MsgPack.Data = GetLobbyData(masterPeer.CurLobby);
                    SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.Success, handleRequest.DeliveryMethod);
                }
            }
        }

        private void LeaveLobbyRequest(HandleRequest handleRequest)
        {
            LeaveLobbyRequest request = MessagePackSerializer.Deserialize<LeaveLobbyRequest>(handleRequest.MsgPack.Data);
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.Peer.Id);
            if (masterPeer != null && masterPeer.IsInLooby)
            {
                masterPeer.CurLobby?.RemoveClientPeer(masterPeer);
                masterPeer.OnLeaveLobby();
                SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.Success, handleRequest.DeliveryMethod);
                return;
            }
            SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.LeaveLobbyFailed, handleRequest.DeliveryMethod);
        }

        private void CreateRoomRequest(HandleRequest handleRequest)
        {
            CreateRoomRequest request = MessagePackSerializer.Deserialize<CreateRoomRequest>(handleRequest.MsgPack.Data);
            MasterPeer? masterPeer = MasterApplication.Instance.GetClientPeer(handleRequest.Peer.Id);
            if (masterPeer != null && masterPeer.IsInLooby)
            {
                Room? room = masterPeer.CurLobby?.CreateRoom(masterPeer, request.RoomName, request.IsVisible, request.NeedPassword, request.Password, request.MaxPlayers, request.RoomProperties);
                if (room != null)
                {
                    handleRequest.MsgPack.Data = GetRoomData(room);
                    SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.Success, handleRequest.DeliveryMethod);
                    return;
                }
            }
            SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.CreateRommFailed, handleRequest.DeliveryMethod);
        }

        private byte[] GetRoomData(Room room)
        {
            JoinRoomResponse response = new JoinRoomResponse();
            response.RoomID = room.RoomID;
            response.RoomProperties = room.RoomProperties;
            foreach (var player in room.ClientPeers)
            {
                response.Players.Add(new Player()
                {
                    ID = player.User.ID,
                    NickName = player.User.NickName,
                });
            }
            return MessagePackSerializer.Serialize(response);
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
                    room.AddClientPeer(masterPeer);
                    masterPeer.OnJoinRoom(room);
                    handleRequest.MsgPack.Data = GetRoomData(room);
                    SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.Success, handleRequest.DeliveryMethod);
                    //call others
                    foreach (var item in room.ClientPeers)
                    {
                        if (item != masterPeer)
                            SendResponse(item.NetPeer, handleRequest.MsgPack, ReturnCode.OnOtherJoinedRoom, handleRequest.DeliveryMethod);
                    }
                    return;
                }
            }
            SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.JoinRoomFailed, handleRequest.DeliveryMethod);
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
                SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.Success, handleRequest.DeliveryMethod);
                if (room != null)
                {
                    handleRequest.MsgPack.Data = GetRoomData(room);
                    //call others
                    foreach (var item in room.ClientPeers)
                    {
                        SendResponse(item.NetPeer, handleRequest.MsgPack, ReturnCode.OnOtherLeaveRoom, handleRequest.DeliveryMethod);
                    }
                }
                return;
            }
            SendResponse(handleRequest.Peer, handleRequest.MsgPack, ReturnCode.LeaveRoomFailed, handleRequest.DeliveryMethod);
        }
    }
}