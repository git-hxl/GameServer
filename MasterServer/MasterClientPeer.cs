using LiteNetLib;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
namespace MasterServer
{
    internal class MasterClientPeer
    {
        private NetPeer? peer;
        private AppLobby? appLobby;

        public string? UserID { get; private set; }
        public string? NickName { get; private set; }
        public MasterClientPeer(NetPeer peer)
        {
            this.peer = peer;
        }
        public OperationResponse OnHandleAuth(OperationRequest operationRequest)
        {
            if (PlayerCache.Instance.ContainsKey(peer.Id))
            {
                return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.AlreadyAuth);
            }

            AuthRequest request = MessagePackSerializer.Deserialize<AuthRequest>(operationRequest.Data);
            try
            {
                string tokenStr = SecurityUtil.AESDecrypt(request.Token, MasterApplication.Instance.MasterServerConfig.encryptKey);
                Token? token = JsonConvert.DeserializeObject<Token>(tokenStr);
                if (token != null)
                {
                    this.UserID = token.UserID;
                    this.NickName = token.NickName;

                    PlayerCache.Instance.AddPlayer(peer.Id, this);

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
                appLobby.LeaveLobby(this);
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
                return operationResponse;
            }

            return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.NotInLobby);
        }

        public OperationResponse OnHandleCreateRoom(OperationRequest operationRequest)
        {
            if (appLobby != null)
            {
                //todo:
                //operationRequest.SendTo(ga)


                OperationResponse operationResponse = OperationResponse.CreateResponse(operationRequest, ReturnCode.CreateRooming, null);
                return operationResponse;
            }

            return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.NotInLobby);
        }

        public void OnDisconnect()
        {
            if (appLobby != null)
                appLobby.LeaveLobby(this);
        }
    }
}
