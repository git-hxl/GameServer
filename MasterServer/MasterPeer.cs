using LiteNetLib;
using MasterServer.Operations;
using Newtonsoft.Json;
using Serilog;
using ShareLibrary;
using ShareLibrary.Utils;
namespace MasterServer
{
    internal class MasterPeer
    {
        public string UserID { get; private set; }
        public NetPeer Peer { get; private set; }
        public MasterPeer(NetPeer netPeer)
        {
            this.Peer = netPeer;
        }


        public OperationResponse OnHandleAuth(OperationRequest operationRequest)
        {
            if (!string.IsNullOrEmpty(UserID))
            {
                return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.AlreadyAuth);
            }

            AuthRequest authRequest = AuthRequest.Deserialize<AuthRequest>(operationRequest.Data);

            try
            {
                string tokenStr = SecurityUtil.AESDecrypt(authRequest.Token, MasterApplication.Instance.ServerConfig.encryptKey);
                Token token = JsonConvert.DeserializeObject<Token>(tokenStr);
                if (token != null)
                {
                    UserID = token.UserID;
                    AuthResponse authResponse = new AuthResponse();
                    authResponse.UserID = token.UserID;
                    authResponse.NickName = token.NickName;
                    return OperationResponse.CreateDefaultResponse(operationRequest, authResponse.Serialize<AuthResponse>());
                }
            }
            catch (Exception ex)
            {
                Log.Information(ex.ToString());
            }
            return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.AuthTokenError);
        }
    }
}
