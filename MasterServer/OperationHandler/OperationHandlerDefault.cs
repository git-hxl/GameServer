
using LiteNetLib;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
using ShareLibrary;
using ShareLibrary.Utils;

using System.Diagnostics;

namespace MasterServer
{
    internal class OperationHandlerDefault
    {
        public OperationResponse OnOperationRequest(NetPeer peer, OperationRequest operationRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            OperationResponse? operationResponse = null;
            Log.Information("request operationcode: {0} ", operationRequest.OperationCode);

            if (operationRequest.OperationCode >= OperationCode.RegisterGameServer)
            {
                operationResponse = OperationHandlerServer.OnOperationRequest(peer, operationRequest);
            }
            else
            {
                MasterClientPeer? masterClientPeer = PlayerCache.Instance.GetPlayer(peer.Id);
                switch (operationRequest.OperationCode)
                {
                    case OperationCode.Auth:
                        operationResponse = OnHandleAuth(peer, operationRequest);
                        break;
                    case OperationCode.JoinLobby:
                        operationResponse = masterClientPeer?.OnHandleJoinLobby(operationRequest);
                        break;
                    case OperationCode.LeaveLobby:
                        operationResponse = masterClientPeer?.OnHandleLeaveLobby(operationRequest);

                        break;
                }
            }

            if (operationResponse == null)
                operationResponse = OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.InvalidOperation);

            stopwatch.Stop();

            Log.Information("request operationcode: {0} returncode {1} stopWatchTime {2}", operationResponse.OperationCode, operationResponse.ReturnCode, stopwatch.ElapsedMilliseconds);

            return operationResponse;
        }

        private OperationResponse OnHandleAuth(NetPeer netPeer, OperationRequest operationRequest)
        {
            if (PlayerCache.Instance.ContainsKey(netPeer.Id))
            {
                return OperationResponse.CreateFailedResponse(operationRequest,ReturnCode.AlreadyAuth);
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
    }
}
