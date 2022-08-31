using LiteNetLib;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
using ShareLibrary;
using ShareLibrary.Utils;
namespace MasterServer
{
    internal class MasterClientPeer
    {
        private NetPeer? netPeer;
        private AppLobby? appLobby;

        public string UserID { get; private set; }
        public MasterClientPeer(NetPeer netPeer, string userID)
        {
            this.netPeer = netPeer;
            this.UserID = userID;
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
