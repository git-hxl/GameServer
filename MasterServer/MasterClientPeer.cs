using LiteNetLib;
using MasterServer.MasterClient.Request;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
using ShareLibrary.Message;
using ShareLibrary.Utils;
namespace MasterServer
{
    internal class MasterClientPeer
    {
        private NetPeer? peer;
        private AppLobby? appLobby;

        public string UserID { get; private set; }
        public MasterClientPeer(NetPeer peer)
        {
            this.peer = peer;
        }
        public OperationResponse OnHandleAuth(OperationRequest operationRequest)
        {
            if (PlayerCache.Instance.ContainsKey(netPeer.Id))
            {
                return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.AlreadyAuth);
            }

            AuthRequest authRequest = MessagePackSerializer.Deserialize<AuthRequest>(operationRequest.Data);
            try
            {
                string tokenStr = SecurityUtil.AESDecrypt(authRequest.Token, MasterApplication.Instance.MasterServerConfig.encryptKey);
                Token? token = JsonConvert.DeserializeObject<Token>(tokenStr);
                if (token != null)
                {
                    MasterClientPeer masterClientPeer = new MasterClientPeer(netPeer, token.UserID);

                    PlayerCache.Instance.AddPlayer(netPeer.Id, masterClientPeer);

                    AuthResponse authResponse = new AuthResponse();
                    authResponse.UserID = token.UserID;
                    authResponse.NickName = token.NickName;

                    byte[] data = MessagePackSerializer.Serialize(authResponse);
                    return OperationResponse.CreateResponse(operationRequest, (byte)ReturnCode.Success, data);
                }
            }
            catch (Exception ex)
            {
                Log.Information(ex.ToString());
            }
            return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.AuthTokenError);
        }

        public OperationResponse OnHandleJoinLobby(OperationRequest operationRequest)
        {
            JoinLobbyRequest joinLobbyRequest = MessagePackSerializer.Deserialize<JoinLobbyRequest>(operationRequest.Data);

            if (appLobby != null)
            {
                appLobby.LeaveLobby(this);
            }

            if (MasterApplication.Instance.LobbyFactory.GetOrCreateLobby(joinLobbyRequest.LobbyName, out appLobby))
            {
                ReturnCode returnCode = appLobby.JoinLobby(this);

                JoinLobbyResponse joinLobbyResponse = new JoinLobbyResponse();
                joinLobbyResponse.LobbyName = joinLobbyRequest.LobbyName;
                byte[] data = MessagePackSerializer.Serialize(joinLobbyResponse);
                OperationResponse operationResponse = OperationResponse.CreateResponse(operationRequest, returnCode, data);

                return operationResponse;
            }

            return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.GetLobbyFailed);
        }

        public OperationResponse OnHandleLeaveLobby(OperationRequest operationRequest)
        {
            LeaveLobbyRequest leaveLobbyRequest = MessagePackSerializer.Deserialize<LeaveLobbyRequest>(operationRequest.Data);

            if (appLobby != null)
            {
                ReturnCode returnCode = appLobby.LeaveLobby(this);
                LeaveLobbyResponse leaveLobbyResponse = new LeaveLobbyResponse();
                leaveLobbyResponse.LobbyName = leaveLobbyRequest.LobbyName;
                byte[] data = MessagePackSerializer.Serialize(leaveLobbyResponse);
                OperationResponse operationResponse = OperationResponse.CreateResponse(operationRequest, returnCode, data);
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
