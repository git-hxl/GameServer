using CommonLibrary.MessagePack;
using CommonLibrary.Operations;
using CommonLibrary.Table;
using Dapper;
using LiteNetLib;
using LiteNetLib.Utils;
using MasterServer.DB;
using MasterServer.Lobby;
using MessagePack;
using System.Diagnostics;

namespace MasterServer.Operations
{
    public class OperationHandleBase
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
                case OperationCode.LevelLobby:
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

                case OperationCode.Disconnect:

                default:
                    SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.InvalidRequest);
                    break;
            }
            Console.WriteLine("{0}：{1} 代码耗时：{2}", DateTime.Now.ToLongTimeString(), handleRequest.OperationCode.ToString(), stopwatch.ElapsedMilliseconds);
        }

        private void SendResponse(ClientPeer clientPeer, OperationCode operationCode, ReturnCode returnCode, byte[]? responseData = null)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)operationCode);
            netDataWriter.Put((byte)returnCode);
            if (responseData != null)
                netDataWriter.Put(responseData);
            clientPeer.NetPeer.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        private async void RegisterRequest(HandleRequest handleRequest)
        {
            RegisterRequestPack registerRequestPack = MessagePackSerializer.Deserialize<RegisterRequestPack>(handleRequest.RequestData);
            using (var dbConnection = await DBHelper.CreateConnection())
            {
                if (dbConnection != null)
                {
                    string queryAccount = $"select * from user where account='{registerRequestPack.Account}'";
                    var existAccount = dbConnection.QueryFirstOrDefault<UserTable>(queryAccount);
                    if (existAccount == null)
                    {
                        string account = registerRequestPack.Account;
                        string password = registerRequestPack.Password;
                        DateTime lastlogintime = DateTime.Now;
                        string insertAccount = $"insert into user (id,account,password,lastlogintime) values ({0},'{account}','{password}','{lastlogintime}')";
                        int result = dbConnection.Execute(insertAccount);
                        if (result == 1)
                        {
                            byte[] responseData = MessagePackSerializer.Serialize<ResponseBasePack>(new ResponseBasePack());
                            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.Success, responseData);
                            return;
                        }
                    }
                }
            }
            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.RegisterFailed);
        }

        private async void LoginRequest(HandleRequest handleRequest)
        {
            LoginRequestPack loginRequestPack = MessagePackSerializer.Deserialize<LoginRequestPack>(handleRequest.RequestData);
            using (var dbConnection = await DBHelper.CreateConnection())
            {
                if (dbConnection != null)
                {
                    string queryThisAccount = $"select * from user where account='{loginRequestPack.Account}'&&password='{loginRequestPack.Password}'";
                    var existAccount = dbConnection.QueryFirstOrDefault<UserTable>(queryThisAccount);
                    if (existAccount != null)
                    {
                        string updateField = $"update user set lastlogintime='{DateTime.Now}' where account='{loginRequestPack.Account}'";
                        dbConnection.Execute(updateField);
                        handleRequest.ClientPeer.Login(existAccount.ID);
                        LoginResponsePack loginResponsePack = new LoginResponsePack();
                        loginResponsePack.ID = existAccount.ID;
                        byte[] responseData = MessagePackSerializer.Serialize<LoginResponsePack>(loginResponsePack);
                        SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.Success, responseData);
                        return;
                    }
                }
            }
            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.LoginFailed);
        }

        private void JoinLobbyRequest(HandleRequest handleRequest)
        {
            JoinLobbyRequestPack joinLobbyRequestPack = MessagePackSerializer.Deserialize<JoinLobbyRequestPack>(handleRequest.RequestData);
            if (!string.IsNullOrEmpty(joinLobbyRequestPack.LobbyName))
            {
                if (handleRequest.ClientPeer.JoinLobby(joinLobbyRequestPack.LobbyName))
                {
                    SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.Success);
                    return;
                }
            }
            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.JoinLobbyFailed);
        }

        private void CreateRoomRequest(HandleRequest handleRequest)
        {
            CreateRoomRequestPack pack = MessagePackSerializer.Deserialize<CreateRoomRequestPack>(handleRequest.RequestData);
            LobbyRoom? lobbyRoom = handleRequest.ClientPeer.CreateRoom(pack.RoomName, pack.IsVisible, pack.Password, pack.MaxPlayers, pack.RoomProperties);
            if (lobbyRoom != null)
            {
                JoinRoomResponsePack joinRoomResponsePack = new JoinRoomResponsePack();
                joinRoomResponsePack.RoomID = lobbyRoom.RoomID;
                joinRoomResponsePack.RoomName = lobbyRoom.RoomName;
                joinRoomResponsePack.Players = lobbyRoom.ClientPeers.Select((a) => a.UserID).ToList();
                byte[] responseData = MessagePackSerializer.Serialize<JoinRoomResponsePack>(joinRoomResponsePack);
                SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.Success, responseData);
                return;
            }
            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.CreateRommFailed);
        }

        private void JoinRoomRequest(HandleRequest handleRequest)
        {
            JoinRoomRequestPack pack = MessagePackSerializer.Deserialize<JoinRoomRequestPack>(handleRequest.RequestData);
            LobbyRoom? lobbyRoom = handleRequest.ClientPeer.JoinRoom(pack.RoomID,pack.Password);
            if (lobbyRoom != null)
            {
                JoinRoomResponsePack joinRoomResponsePack = new JoinRoomResponsePack();
                joinRoomResponsePack.RoomID = lobbyRoom.RoomID;
                joinRoomResponsePack.RoomName = lobbyRoom.RoomName;
                joinRoomResponsePack.Players = lobbyRoom.ClientPeers.Select((a) => a.UserID).ToList();
                byte[] responseData = MessagePackSerializer.Serialize<JoinRoomResponsePack>(joinRoomResponsePack);
                SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.Success, responseData);
                return;
            }
            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.JoinRoomFailed);
        }

        private void LeaveRoomRequest(HandleRequest handleRequest)
        {
            RequsetBasePack pack = MessagePackSerializer.Deserialize<RequsetBasePack>(handleRequest.RequestData);
            if (handleRequest.ClientPeer.LeaveRoom())
            {
                ResponseBasePack responseBasePack = new ResponseBasePack();
                byte[] responseData = MessagePackSerializer.Serialize<ResponseBasePack>(responseBasePack);
                SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.Success, responseData);
                return;
            }
            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.LeaveRoomFailed);
        }
    }
}