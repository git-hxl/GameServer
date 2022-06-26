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
                        DateTime lastlogintime = DateTime.UtcNow;
                        string insertAccount = $"insert into user (id,account,password,lastlogintime) values ({0},'{account}','{password}','{lastlogintime.ToString()}')";
                        int result = dbConnection.Execute(insertAccount);
                        if (result == 1)
                        {
                            //string queryThisAccount = $"select * from user where account='{account}'&&password='{password}'";
                            //existAccount = dbConnection.QueryFirstOrDefault<UserTable>(queryThisAccount);
                            //handleRequest.ClientPeer.UserID = existAccount.ID;
                            //LoginResponsePack loginResponsePack = new LoginResponsePack();
                            //loginResponsePack.ID = existAccount.ID;
                            //byte[] responseData = MessagePackSerializer.Serialize<LoginResponsePack>(loginResponsePack);
                            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.Success);
                            return;
                        }
                    }
                }
            }
            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.RegisterFailed);
        }

        private async void LoginRequest(HandleRequest handleRequest)
        {
            if (handleRequest.ClientPeer.IsLogin == false)
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
                            string updateField = $"update user set lastlogintime='{DateTime.UtcNow.ToString()}' where account='{loginRequestPack.Account}'";
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
            }
            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.LoginFailed);
        }

        private void JoinLobbyRequest(HandleRequest handleRequest)
        {
            if (handleRequest.ClientPeer.IsLogin)
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
            }
            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.JoinLobbyFailed);
        }

        private void CreateRoomRequest(HandleRequest handleRequest)
        {
            if (handleRequest.ClientPeer.IsLogin && handleRequest.ClientPeer.IsInLobby)
            {
                CreateRoomRequestPack pack = MessagePackSerializer.Deserialize<CreateRoomRequestPack>(handleRequest.RequestData);
                LobbyRoom? lobbyRoom = handleRequest.ClientPeer.CreateRoom(pack.RoomName, pack.MaxPlayers, pack.RoomProperties);
                if (lobbyRoom != null)
                {
                    if (handleRequest.ClientPeer.JoinRoom(lobbyRoom.RoomID, out lobbyRoom))
                    {
                        JoinRoomResponsePack joinRoomResponsePack = new JoinRoomResponsePack();
                        joinRoomResponsePack.RoomID = lobbyRoom.RoomID;
                        joinRoomResponsePack.RoomName = lobbyRoom.RoomName;
                        joinRoomResponsePack.OwnerID = lobbyRoom.Owner.UserID;
                        joinRoomResponsePack.Players = lobbyRoom.ClientPeers.Select((a) => a.UserID).ToList();
                        byte[] responseData = MessagePackSerializer.Serialize<JoinRoomResponsePack>(joinRoomResponsePack);
                        SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.Success, responseData);
                        return;
                    }
                }
            }
            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.CreateRommFailed);
        }

        private void JoinRoomRequest(HandleRequest handleRequest)
        {
            if (handleRequest.ClientPeer.IsLogin && handleRequest.ClientPeer.IsInLobby)
            {
                JoinRoomRequestPack pack = MessagePackSerializer.Deserialize<JoinRoomRequestPack>(handleRequest.RequestData);
                LobbyRoom? lobbyRoom;
                if (handleRequest.ClientPeer.JoinRoom(pack.RoomID, out lobbyRoom))
                {
                    JoinRoomResponsePack joinRoomResponsePack = new JoinRoomResponsePack();
                    joinRoomResponsePack.RoomID = lobbyRoom.RoomID;
                    joinRoomResponsePack.RoomName = lobbyRoom.RoomName;
                    joinRoomResponsePack.OwnerID = lobbyRoom.Owner.UserID;
                    joinRoomResponsePack.Players = lobbyRoom.ClientPeers.Select((a) => a.UserID).ToList();
                    byte[] responseData = MessagePackSerializer.Serialize<JoinRoomResponsePack>(joinRoomResponsePack);
                    SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.Success, responseData);
                    return;
                }
            }
            SendResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ReturnCode.JoinRoomFailed);
        }
    }
}