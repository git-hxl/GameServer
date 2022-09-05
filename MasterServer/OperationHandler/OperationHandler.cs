using Serilog;
using System.Diagnostics;

namespace MasterServer
{
    internal class OperationHandler
    {
        public OperationResponse OnOperationRequest(MasterClientPeer peer, OperationRequest operationRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            OperationResponse operationResponse = null;

            switch (operationRequest.OperationCode)
            {
                case OperationCode.Auth:
                    operationResponse = peer.OnHandleAuth(operationRequest);
                    break;
                case OperationCode.JoinLobby:
                    operationResponse = peer.OnHandleJoinLobby(operationRequest);
                    break;
                case OperationCode.LeaveLobby:
                    operationResponse = peer.OnHandleLeaveLobby(operationRequest);
                    break;
                case OperationCode.CreateRoom:
                    operationResponse = peer.OnHandleCreateRoom(operationRequest);
                    break;
            }

            if (operationResponse == null)
                operationResponse = OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.InvalidOperation);

            stopwatch.Stop();

            Log.Information("request operationcode: {0} returncode {1} stopWatchTime {2}", operationResponse.OperationCode, operationResponse.ReturnCode, stopwatch.ElapsedMilliseconds);

            return operationResponse;
        }

        public OperationResponse OnOperationRequest(GameServerPeer peer, OperationRequest operationRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            OperationResponse operationResponse = null;
            switch (operationRequest.OperationCode)
            {
                case OperationCode.RegisterGameServer:
                    operationResponse = peer.OnRegisterGameServer(operationRequest);
                    break;
                case OperationCode.UpdateGameServer:
                    operationResponse = peer.OnUpdateGameServer(operationRequest);
                    break;
            }

            if (operationResponse == null)
                operationResponse = OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.InvalidOperation);

            stopwatch.Stop();

            Log.Information("request operationcode: {0} stopWatchTime {1}", operationResponse.OperationCode, stopwatch.ElapsedMilliseconds);

            return operationResponse;
        }

        public void OnOperationResponse(GameServerPeer peer, OperationResponse operationResponse)
        {
            Log.Information("response operationcode {0} returncode {1}", operationResponse.OperationCode, operationResponse.ReturnCode);

            switch (operationResponse.OperationCode)
            {
                case OperationCode.CreateRoom:
                    peer.OnCreateRoomResponse(operationResponse);
                    break;
            }
        }
    }
}