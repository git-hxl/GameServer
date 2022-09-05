using LiteNetLib;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
namespace MasterServer
{
    internal class MasterClientPeer
    {
        public NetPeer? Peer { get; private set; }

        public RoomState Room { get; set; }

        private AppLobby? appLobby;

        public string? UserID { get; private set; }
        public string? NickName { get; private set; }
        public MasterClientPeer(NetPeer peer)
        {
            this.Peer = peer;
        }
        public OperationResponse OnHandleAuth(OperationRequest operationRequest)
        {
            AuthRequest request = MessagePackSerializer.Deserialize<AuthRequest>(operationRequest.Data);
            try
            {
                string tokenStr = SecurityUtil.AESDecrypt(request.Token, MasterApplication.Instance.MasterServerConfig.encryptKey);
                Token? token = JsonConvert.DeserializeObject<Token>(tokenStr);
                if (token != null)
                {
                    if (PlayerCache.Instance.ContainsKey(token.UserID))
                    {
                        return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.AlreadyAuth);
                    }


                    this.UserID = token.UserID;
                    this.NickName = token.NickName;

                    PlayerCache.Instance.AddPlayer(UserID, this);

                    AuthResponse response = new AuthResponse();
                    response.UserID = token.UserID;
                    response.NickName = token.NickName;

                    byte[] data = MessagePackSerializer.Serialize(response);
                    return OperationResponse.CreateResponse(operationRequest, (byte)ReturnCode.Success, data);
                }
            }
            catch (Exception ex)
            {
                Log.Information(ex.Message);
            }
            return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.AuthTokenError);
        }

        public OperationResponse OnHandleJoinLobby(OperationRequest operationRequest)
        {
            JoinLobbyRequest request = MessagePackSerializer.Deserialize<JoinLobbyRequest>(operationRequest.Data);

            if (appLobby != null)
            {
                return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.AlreadyJoinLobby);
            }

            if (LobbyFactory.Instance.GetOrCreateLobby(request.LobbyName, out appLobby))
            {
                ReturnCode returnCode = appLobby.JoinLobby(this);

                JoinLobbyResponse response = new JoinLobbyResponse();
                response.LobbyName = request.LobbyName;
                byte[] data = MessagePackSerializer.Serialize(response);
                OperationResponse operationResponse = OperationResponse.CreateResponse(operationRequest, returnCode, data);

                return operationResponse;
            }

            return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.GetLobbyFailed);
        }

        public OperationResponse OnHandleLeaveLobby(OperationRequest operationRequest)
        {
            LeaveLobbyRequest request = MessagePackSerializer.Deserialize<LeaveLobbyRequest>(operationRequest.Data);

            if (appLobby != null)
            {
                ReturnCode returnCode = appLobby.LeaveLobby(this);
                LeaveLobbyResponse response = new LeaveLobbyResponse();
                response.LobbyName = request.LobbyName;
                byte[] data = MessagePackSerializer.Serialize(response);
                OperationResponse operationResponse = OperationResponse.CreateResponse(operationRequest, returnCode, data);

                appLobby = null;

                return operationResponse;
            }

            return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.NotInLobby);
        }

        public OperationResponse OnHandleCreateRoom(OperationRequest operationRequest)
        {
            if (appLobby != null)
            {
                GameServerPeer gameServerPeer = GameServerCache.Instance.GetMinServer();
                if (gameServerPeer == null)
                {
                    return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.NoMatchGameServer);
                }

                if (Room != null)
                {
                    return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.AlreadyInRoom);
                }

                operationRequest.SendTo(gameServerPeer.Peer);

                OperationResponse operationResponse = OperationResponse.CreateResponse(operationRequest, ReturnCode.CreateRooming, null);
                return operationResponse;
            }

            return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.NotInLobby);
        }

        public void OnDisconnect()
        {
            if (appLobby != null)
                appLobby.LeaveLobby(this);

            PlayerCache.Instance.RemovePlayer(UserID);
        }
    }
}
